using Microsoft.Extensions.Logging;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Interfaces;
using SermonTranscription.Domain.Exceptions;
using BCrypt.Net;
using SermonTranscription.Application.DTOs;

namespace SermonTranscription.Application.Services;

/// <summary>
/// Authentication service for user authentication and authorization
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserOrganizationRepository _userOrganizationRepository;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthService> _logger;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordValidator _passwordValidator;

    public AuthService(
        IUserRepository userRepository,
        IUserOrganizationRepository userOrganizationRepository,
        IJwtService jwtService,
        ILogger<AuthService> logger,
        IPasswordHasher passwordHasher,
        IPasswordValidator passwordValidator)
    {
        _userRepository = userRepository;
        _userOrganizationRepository = userOrganizationRepository;
        _jwtService = jwtService;
        _logger = logger;
        _passwordHasher = passwordHasher;
        _passwordValidator = passwordValidator;
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return AuthResult.Failure("Email and password are required");
            }

            // Find user by email
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("Login attempt with non-existent email: {Email}", email);
                return AuthResult.Failure("Invalid email or password");
            }

            // Validate user is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Login attempt for inactive user: {UserId}", user.Id);
                return AuthResult.Failure("Account is deactivated");
            }

            // Validate email is verified
            if (!user.IsEmailVerified)
            {
                _logger.LogWarning("Login attempt for unverified email: {UserId}", user.Id);
                return AuthResult.Failure("Email address not verified");
            }

            // Verify password (assuming BCrypt is used)
            if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
            {
                _logger.LogWarning("Login attempt with invalid password for user: {UserId}", user.Id);
                return AuthResult.Failure("Invalid email or password");
            }

            // Get user's first organization membership
            var memberships = await _userOrganizationRepository.GetUserOrganizationsAsync(user.Id);
            var primaryMembership = memberships.FirstOrDefault(m => m.IsActive);
            if (primaryMembership == null)
            {
                _logger.LogWarning("User {UserId} has no active organization membership", user.Id);
                return AuthResult.Failure("User is not associated with any organization");
            }

            // Generate tokens (JWT contains only user identity, no tenant info)
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = await IssueRefreshTokenAsync(user);

            // Update user's last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Successful login for user {UserId}", user.Id);

            return AuthResult.Success(accessToken, refreshToken, new AuthUserInfo
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
                // OrganizationId and Role are no longer included as they're determined per-request
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", email);
            return AuthResult.Failure("An error occurred during login");
        }
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return AuthResult.Failure("Email and password are required");
            }

            if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            {
                return AuthResult.Failure("First name and last name are required");
            }

            _passwordValidator.Validate(request.Password);

            // Check if user already exists
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return AuthResult.Failure("User with this email already exists");
            }

            // Create new user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email.ToLowerInvariant(),
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                IsEmailVerified = false, // Will be verified via email
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);

            _logger.LogInformation("New user registered: {UserId}", user.Id);

            return AuthResult.Success("User registered successfully. Please check your email to verify your account.");
        }
        catch (PasswordValidationDomainException ex)
        {
            _logger.LogWarning("Password validation failed during registration for email: {Email}", request.Email);
            return AuthResult.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for email: {Email}", request.Email);
            return AuthResult.Failure("An error occurred during registration");
        }
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return AuthResult.Failure("Refresh token is required");
            }

            // Get refresh token from database
            var storedRefreshToken = await _userRepository.GetRefreshTokenAsync(refreshToken);
            if (storedRefreshToken == null)
            {
                _logger.LogWarning("Refresh token not found: {Token}", refreshToken);
                return AuthResult.Failure("Invalid refresh token");
            }

            // Check if token is expired
            if (storedRefreshToken.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Refresh token expired for user: {UserId}", storedRefreshToken.UserId);
                await _userRepository.RevokeRefreshTokenAsync(refreshToken);
                return AuthResult.Failure("Refresh token has expired");
            }

            // Check if token is revoked
            if (storedRefreshToken.RevokedAt.HasValue)
            {
                _logger.LogWarning("Refresh token revoked for user: {UserId}", storedRefreshToken.UserId);
                return AuthResult.Failure("Refresh token has been revoked");
            }

            // Get user and validate they are still active
            var user = storedRefreshToken.User;
            if (!user.IsActive)
            {
                _logger.LogWarning("Refresh token used for inactive user: {UserId}", user.Id);
                await _userRepository.RevokeAllUserRefreshTokensAsync(user.Id);
                return AuthResult.Failure("User account is deactivated");
            }

            // Get user's primary organization membership
            var memberships = await _userOrganizationRepository.GetUserOrganizationsAsync(user.Id);
            var primaryMembership = memberships.FirstOrDefault(m => m.IsActive);
            if (primaryMembership == null)
            {
                _logger.LogWarning("User {UserId} has no active organization membership", user.Id);
                return AuthResult.Failure("User is not associated with any organization");
            }

            // Generate new tokens (JWT contains only user identity, no tenant info)
            var newAccessToken = _jwtService.GenerateAccessToken(user);
            var newRefreshToken = _jwtService.GenerateRefreshToken(user);

            // Revoke the old refresh token and add the new one
            await _userRepository.RevokeRefreshTokenAsync(refreshToken);

            var newStoredRefreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(30), // 30 days expiry
                CreatedAt = DateTime.UtcNow
            };
            await _userRepository.AddRefreshTokenAsync(newStoredRefreshToken);

            // Update user's last login
            user.UpdateLastLogin();
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Successfully refreshed tokens for user {UserId}", user.Id);

            return AuthResult.Success(newAccessToken, newRefreshToken, new AuthUserInfo
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
                // OrganizationId and Role are no longer included as they're determined per-request
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return AuthResult.Failure("An error occurred during token refresh");
        }
    }

    public async Task<AuthResult> RevokeRefreshTokenAsync(string refreshToken)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return AuthResult.Failure("Refresh token is required");
            }

            // Revoke the refresh token
            await _userRepository.RevokeRefreshTokenAsync(refreshToken);

            _logger.LogInformation("Refresh token revoked: {Token}", refreshToken);

            return AuthResult.Success("Refresh token revoked successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking refresh token");
            return AuthResult.Failure("An error occurred while revoking the refresh token");
        }
    }

    public async Task<AuthResult> RevokeAllUserRefreshTokensAsync(Guid userId)
    {
        try
        {
            // Revoke all refresh tokens for the user
            await _userRepository.RevokeAllUserRefreshTokensAsync(userId);

            _logger.LogInformation("All refresh tokens revoked for user: {UserId}", userId);

            return AuthResult.Success("All refresh tokens revoked successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all refresh tokens for user {UserId}", userId);
            return AuthResult.Failure("An error occurred while revoking refresh tokens");
        }
    }

    private async Task<string> IssueRefreshTokenAsync(User user)
    {
        var refreshToken = _jwtService.GenerateRefreshToken(user);

        var storedRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(30), // 30 days expiry
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddRefreshTokenAsync(storedRefreshToken);

        return refreshToken;
    }



    public async Task<AuthResult> ForgotPasswordAsync(string email)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(email))
            {
                return AuthResult.Failure("Email is required");
            }

            // Find user by email
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                // Don't reveal if user exists or not for security
                _logger.LogInformation("Password reset requested for email: {Email} (user not found)", email);
                return AuthResult.Success("If the email address exists in our system, you will receive a password reset link.");
            }

            // Validate user is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Password reset requested for inactive user: {UserId}", user.Id);
                return AuthResult.Success("If the email address exists in our system, you will receive a password reset link.");
            }

            // Generate password reset token (valid for 1 hour)
            var resetToken = GeneratePasswordResetToken();
            var resetTokenExpiry = DateTime.UtcNow.AddHours(1);

            // Store reset token in user entity (in a real implementation, you might want a separate table)
            user.PasswordResetToken = resetToken;
            user.PasswordResetTokenExpiry = resetTokenExpiry;
            await _userRepository.UpdateAsync(user);

            // TODO: Send email with reset link
            // In a real implementation, you would inject an email service and send the reset link
            _logger.LogInformation("Password reset token generated for user {UserId}: {Token}", user.Id, resetToken);

            return AuthResult.Success("If the email address exists in our system, you will receive a password reset link.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot password for email: {Email}", email);
            return AuthResult.Failure("An error occurred while processing your request");
        }
    }

    public async Task<AuthResult> ResetPasswordAsync(string token, string newPassword)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword))
            {
                return AuthResult.Failure("Token and new password are required");
            }

            _passwordValidator.Validate(newPassword);

            // Find user by reset token
            var user = await _userRepository.GetByPasswordResetTokenAsync(token);
            if (user == null)
            {
                _logger.LogWarning("Password reset attempted with invalid token: {Token}", token);
                return AuthResult.Failure("Invalid or expired reset token");
            }

            // Check if token is expired
            if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
            {
                _logger.LogWarning("Password reset attempted with expired token for user: {UserId}", user.Id);
                return AuthResult.Failure("Reset token has expired");
            }

            // Update password and clear reset token
            user.PasswordHash = _passwordHasher.HashPassword(newPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Password successfully reset for user {UserId}", user.Id);

            return AuthResult.Success("Password has been successfully reset");
        }
        catch (PasswordValidationDomainException ex)
        {
            _logger.LogWarning("Password validation failed during password reset");
            return AuthResult.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset");
            return AuthResult.Failure("An error occurred while resetting your password");
        }
    }

    private static string GeneratePasswordResetToken()
    {
        // Generate a secure random token
        var randomBytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    public async Task<List<OrganizationSummaryDto>> GetUserOrganizationsAsync(Guid userId)
    {
        try
        {
            // Get user with organization memberships
            var user = await _userRepository.GetByIdWithOrganizationsAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return new List<OrganizationSummaryDto>();
            }

            // Get active organization memberships
            var memberships = await _userOrganizationRepository.GetUserOrganizationsAsync(userId);
            var activeMemberships = memberships.Where(m => m.IsActive).ToList();

            // Convert to DTOs
            var organizationDtos = activeMemberships.Select(membership => new OrganizationSummaryDto
            {
                Id = membership.Organization.Id,
                Name = membership.Organization.Name,
                Role = membership.Role.ToString(),
                IsActive = membership.IsActive
            }).ToList();

            _logger.LogDebug("Retrieved {Count} organizations for user {UserId}", organizationDtos.Count, userId);
            return organizationDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organizations for user {UserId}", userId);
            throw;
        }
    }
}

/// <summary>
/// Authentication service interface
/// </summary>
public interface IAuthService
{
    Task<AuthResult> LoginAsync(string email, string password);
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    Task<AuthResult> RefreshTokenAsync(string refreshToken);
    Task<AuthResult> RevokeRefreshTokenAsync(string refreshToken);
    Task<AuthResult> RevokeAllUserRefreshTokensAsync(Guid userId);
    Task<AuthResult> ForgotPasswordAsync(string email);
    Task<AuthResult> ResetPasswordAsync(string token, string newPassword);
    Task<List<OrganizationSummaryDto>> GetUserOrganizationsAsync(Guid userId);
}

/// <summary>
/// Authentication result
/// </summary>
public class AuthResult
{
    public bool IsSuccess { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public AuthUserInfo? User { get; private set; }

    private AuthResult(bool isSuccess, string message)
    {
        IsSuccess = isSuccess;
        Message = message;
    }

    public static AuthResult Success(string message) => new(true, message);

    public static AuthResult Success(string accessToken, string refreshToken, AuthUserInfo user) => new(true, "Login successful")
    {
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        User = user
    };

    public static AuthResult Success(AuthUserInfo user) => new(true, "Token validation successful")
    {
        User = user
    };

    public static AuthResult Failure(string message) => new(false, message);
}
