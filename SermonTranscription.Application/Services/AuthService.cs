using Microsoft.Extensions.Logging;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Interfaces;
using SermonTranscription.Domain.Exceptions;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Common;

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

    public async Task<ServiceResult<LoginResponse>> LoginAsync(string email, string password)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return ServiceResult<LoginResponse>.Failure("Email and password are required", "VALIDATION_ERROR", "email");
            }

            // Find user by email
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("Login attempt with non-existent email: {Email}", email);
                return ServiceResult<LoginResponse>.Failure("Invalid email or password", "UNAUTHORIZED");
            }

            // Validate user is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Login attempt for inactive user: {UserId}", user.Id);
                return ServiceResult<LoginResponse>.Failure("Account is deactivated", "FORBIDDEN");
            }

            // Validate email is verified
            if (!user.IsEmailVerified)
            {
                _logger.LogWarning("Login attempt for unverified email: {UserId}", user.Id);
                return ServiceResult<LoginResponse>.Failure("Email address not verified", "UNAUTHORIZED");
            }

            // Verify password (assuming BCrypt is used)
            if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
            {
                _logger.LogWarning("Login attempt with invalid password for user: {UserId}", user.Id);
                return ServiceResult<LoginResponse>.Failure("Invalid email or password", "UNAUTHORIZED");
            }

            // Get user's first organization membership
            var memberships = await _userOrganizationRepository.GetUserOrganizationsAsync(user.Id);
            var primaryMembership = memberships.FirstOrDefault(m => m.IsActive);
            if (primaryMembership == null)
            {
                _logger.LogWarning("User {UserId} has no active organization membership", user.Id);
                return ServiceResult<LoginResponse>.Failure("User is not associated with any organization", "FORBIDDEN");
            }

            // Generate tokens (JWT contains only user identity, no tenant info)
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = await IssueRefreshTokenAsync(user);

            // Update user's last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Successful login for user {UserId}", user.Id);

            var response = new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = new AuthUserInfo
                {
                    UserId = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName
                }
            };

            return ServiceResult<LoginResponse>.Success(response, "Login successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", email);
            return ServiceResult<LoginResponse>.Failure("An error occurred during login", "INTERNAL_ERROR");
        }
    }

    public async Task<ServiceResult<RegisterResponse>> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return ServiceResult<RegisterResponse>.Failure("Email and password are required", "VALIDATION_ERROR", "email");
            }

            // Check if user already exists
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return ServiceResult<RegisterResponse>.Failure("User with this email already exists", "CONFLICT", "email", request.Email);
            }

            // Validate password
            try
            {
                _passwordValidator.Validate(request.Password);
            }
            catch (PasswordValidationDomainException ex)
            {
                return ServiceResult<RegisterResponse>.Failure(ex.Message, "VALIDATION_ERROR", "password");
            }

            // Hash password
            var passwordHash = _passwordHasher.HashPassword(request.Password);

            // Create new user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordHash = passwordHash,
                IsActive = true,
                IsEmailVerified = false, // Will be verified via email
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);

            _logger.LogInformation("User registered successfully: {UserId}", user.Id);

            var response = new RegisterResponse
            {
                Message = "Registration successful. Please check your email to verify your account."
            };

            return ServiceResult<RegisterResponse>.Success(response, "Registration successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for email: {Email}", request.Email);
            return ServiceResult<RegisterResponse>.Failure("An error occurred during registration", "INTERNAL_ERROR");
        }
    }

    public async Task<ServiceResult<RefreshResponse>> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return ServiceResult<RefreshResponse>.Failure("Refresh token is required", "VALIDATION_ERROR", "refreshToken");
            }

            // Find refresh token in database
            var tokenEntity = await _userRepository.GetRefreshTokenAsync(refreshToken);
            if (tokenEntity == null)
            {
                _logger.LogWarning("Refresh token not found: {Token}", refreshToken);
                return ServiceResult<RefreshResponse>.Failure("Invalid refresh token", "UNAUTHORIZED");
            }

            // Check if token is expired
            if (tokenEntity.ExpiresAt < DateTime.UtcNow)
            {
                await _userRepository.RevokeRefreshTokenAsync(refreshToken);
                return ServiceResult<RefreshResponse>.Failure("Refresh token has expired", "UNAUTHORIZED");
            }

            // Check if token is revoked
            if (tokenEntity.RevokedAt.HasValue)
            {
                _logger.LogWarning("Refresh token revoked for user: {UserId}", tokenEntity.UserId);
                return ServiceResult<RefreshResponse>.Failure("Refresh token has been revoked", "UNAUTHORIZED");
            }

            // Get user
            var user = tokenEntity.User;

            if (!user.IsActive)
            {
                _logger.LogWarning("Refresh token used for inactive user: {UserId}", user.Id);
                await _userRepository.RevokeAllUserRefreshTokensAsync(user.Id);
                return ServiceResult<RefreshResponse>.Failure("User account is deactivated", "FORBIDDEN");
            }

            // Generate new tokens
            var newAccessToken = _jwtService.GenerateAccessToken(user);
            var newRefreshToken = await IssueRefreshTokenAsync(user);

            // Revoke old refresh token
            await _userRepository.RevokeRefreshTokenAsync(refreshToken);

            _logger.LogInformation("Token refreshed successfully for user {UserId}", user.Id);

            var response = new RefreshResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };

            _logger.LogInformation("Successfully refreshed tokens for user {UserId}", user.Id);
            return ServiceResult<RefreshResponse>.Success(response, "Token refreshed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return ServiceResult<RefreshResponse>.Failure("An error occurred during token refresh", "INTERNAL_ERROR");
        }
    }

    public async Task<ServiceResult<LogoutResponse>> RevokeRefreshTokenAsync(string refreshToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return ServiceResult<LogoutResponse>.Failure("Refresh token is required", "VALIDATION_ERROR", "refreshToken");
            }

            await _userRepository.RevokeRefreshTokenAsync(refreshToken);

            _logger.LogInformation("Refresh token revoked: {Token}", refreshToken);

            var response = new LogoutResponse
            {
                Message = "Logout successful"
            };

            return ServiceResult<LogoutResponse>.Success(response, "Logout successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return ServiceResult<LogoutResponse>.Failure("An error occurred during logout", "INTERNAL_ERROR");
        }
    }

    public async Task<ServiceResult<LogoutResponse>> RevokeAllUserRefreshTokensAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult<LogoutResponse>.Failure("User not found", "NOT_FOUND");
            }

            await _userRepository.RevokeAllUserRefreshTokensAsync(userId);

            _logger.LogInformation("All refresh tokens revoked for user {UserId}", userId);

            var response = new LogoutResponse
            {
                Message = "All refresh tokens revoked successfully"
            };

            return ServiceResult<LogoutResponse>.Success(response, "All refresh tokens revoked successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all refresh tokens for user {UserId}", userId);
            return ServiceResult<LogoutResponse>.Failure("An error occurred while revoking refresh tokens", "INTERNAL_ERROR");
        }
    }

    private async Task<string> IssueRefreshTokenAsync(User user)
    {
        var refreshToken = _jwtService.GenerateRefreshToken(user);
        var tokenEntity = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddRefreshTokenAsync(tokenEntity);
        return refreshToken;
    }

    public async Task<ServiceResult<ForgotPasswordResponse>> ForgotPasswordAsync(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return ServiceResult<ForgotPasswordResponse>.Failure("Email is required", "VALIDATION_ERROR", "email");
            }

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                // Don't reveal if user exists or not for security
                _logger.LogInformation("Password reset requested for non-existent email: {Email}", email);

                return ServiceResult<ForgotPasswordResponse>.Success(new ForgotPasswordResponse
                {
                    Message = "If an account with this email exists, a password reset link has been sent."
                }, "Password reset email sent");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Password reset requested for inactive user: {UserId}", user.Id);
                return ServiceResult<ForgotPasswordResponse>.Success(new ForgotPasswordResponse
                {
                    Message = "If an account with this email exists, a password reset link has been sent."
                }, "Password reset email sent");
            }

            // Generate reset token
            var resetToken = GeneratePasswordResetToken();
            user.PasswordResetToken = resetToken;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            // TODO: Send email with reset link
            _logger.LogInformation("Password reset token generated for user {UserId}: {Token}", user.Id, resetToken);

            return ServiceResult<ForgotPasswordResponse>.Success(new ForgotPasswordResponse
            {
                Message = "If an account with this email exists, a password reset link has been sent."
            }, "Password reset email sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot password for email: {Email}", email);
            return ServiceResult<ForgotPasswordResponse>.Failure("An error occurred while processing the request", "INTERNAL_ERROR");
        }
    }

    public async Task<ServiceResult<ResetPasswordResponse>> ResetPasswordAsync(string token, string newPassword)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword))
            {
                return ServiceResult<ResetPasswordResponse>.Failure("Token and new password are required", "VALIDATION_ERROR");
            }

            // Validate new password
            try
            {
                _passwordValidator.Validate(newPassword);
            }
            catch (PasswordValidationDomainException ex)
            {
                return ServiceResult<ResetPasswordResponse>.Failure(ex.Message, "VALIDATION_ERROR", "newPassword");
            }
            // Find user by reset token
            var user = await _userRepository.GetByPasswordResetTokenAsync(token);
            if (user == null)
            {
                return ServiceResult<ResetPasswordResponse>.Failure("Invalid or expired reset token", "UNAUTHORIZED");
            }

            // Check if token is expired
            if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
            {
                _logger.LogWarning("Password reset attempted with expired token for user: {UserId}", user.Id);
                return ServiceResult<ResetPasswordResponse>.Failure("Reset token has expired", "UNAUTHORIZED");
            }

            // Hash new password
            var passwordHash = _passwordHasher.HashPassword(newPassword);

            // Update user
            user.PasswordHash = passwordHash;
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Password reset successfully for user {UserId}", user.Id);

            var response = new ResetPasswordResponse
            {
                Message = "Password has been reset successfully"
            };

            return ServiceResult<ResetPasswordResponse>.Success(response, "Password reset successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset");
            return ServiceResult<ResetPasswordResponse>.Failure("An error occurred while resetting the password", "INTERNAL_ERROR");
        }
    }

    private static string GeneratePasswordResetToken()
    {
        return Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    public async Task<ServiceResult<List<OrganizationSummaryDto>>> GetUserOrganizationsAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult<List<OrganizationSummaryDto>>.Failure("User not found", "NOT_FOUND");
            }

            var memberships = await _userOrganizationRepository.GetUserOrganizationsAsync(userId);
            var organizations = memberships
                .Where(m => m.IsActive && m.Organization.IsActive)
                .Select(m => new OrganizationSummaryDto
                {
                    Id = m.Organization.Id,
                    Name = m.Organization.Name,
                    Slug = m.Organization.Slug,
                    Role = m.Role.ToString(),
                    IsActive = m.IsActive
                })
                .ToList();

            return ServiceResult<List<OrganizationSummaryDto>>.Success(organizations, "User organizations retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organizations for user {UserId}", userId);
            return ServiceResult<List<OrganizationSummaryDto>>.Failure("An error occurred while retrieving user organizations", "INTERNAL_ERROR");
        }
    }
}

public interface IAuthService
{
    Task<ServiceResult<LoginResponse>> LoginAsync(string email, string password);
    Task<ServiceResult<RegisterResponse>> RegisterAsync(RegisterRequest request);
    Task<ServiceResult<RefreshResponse>> RefreshTokenAsync(string refreshToken);
    Task<ServiceResult<LogoutResponse>> RevokeRefreshTokenAsync(string refreshToken);
    Task<ServiceResult<LogoutResponse>> RevokeAllUserRefreshTokensAsync(Guid userId);
    Task<ServiceResult<ForgotPasswordResponse>> ForgotPasswordAsync(string email);
    Task<ServiceResult<ResetPasswordResponse>> ResetPasswordAsync(string token, string newPassword);
    Task<ServiceResult<List<OrganizationSummaryDto>>> GetUserOrganizationsAsync(Guid userId);
}
