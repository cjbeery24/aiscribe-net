using Microsoft.Extensions.Logging;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Interfaces;
using SermonTranscription.Domain.Exceptions;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Common;
using SermonTranscription.Application.Interfaces;
using SermonTranscription.Domain.Common;

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

    public async Task<ServiceResult<LoginResponse>> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return ServiceResult<LoginResponse>.Failure("Email and password are required", ErrorCode.ValidationError, "email");
            }

            // Find user by email
            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Login attempt with non-existent email: {Email}", email);
                return ServiceResult<LoginResponse>.Failure("Invalid email or password", ErrorCode.Unauthorized);
            }

            // Validate user is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Login attempt for inactive user: {UserId}", user.Id);
                return ServiceResult<LoginResponse>.Failure("Account is deactivated", ErrorCode.Forbidden);
            }

            // Validate email is verified
            if (!user.IsEmailVerified)
            {
                _logger.LogWarning("Login attempt for unverified email: {UserId}", user.Id);
                return ServiceResult<LoginResponse>.Failure("Email address not verified", ErrorCode.Unauthorized);
            }

            // Verify password (assuming BCrypt is used)
            if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
            {
                _logger.LogWarning("Login attempt with invalid password for user: {UserId}", user.Id);
                return ServiceResult<LoginResponse>.Failure("Invalid email or password", ErrorCode.Unauthorized);
            }

            // Get user's first organization membership
            var memberships = await _userOrganizationRepository.GetUserOrganizationsAsync(user.Id);
            var primaryMembership = memberships.FirstOrDefault(m => m.IsActive);
            if (primaryMembership == null)
            {
                _logger.LogWarning("User {UserId} has no active organization membership", user.Id);
                return ServiceResult<LoginResponse>.Failure("User is not associated with any organization", ErrorCode.Forbidden);
            }

            // Generate tokens (JWT contains only user identity, no tenant info)
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = await IssueRefreshTokenAsync(user, cancellationToken);

            // Update user's last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user, cancellationToken);

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
            return ServiceResult<LoginResponse>.Failure("An error occurred during login", ErrorCode.InternalError);
        }
    }

    public async Task<ServiceResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return ServiceResult<RegisterResponse>.Failure("Email and password are required", ErrorCode.ValidationError, "email");
            }

            // Check if user already exists
            var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (existingUser != null)
            {
                return ServiceResult<RegisterResponse>.Failure("User with this email already exists", ErrorCode.Conflict, "email", request.Email);
            }

            // Validate password
            try
            {
                _passwordValidator.Validate(request.Password);
            }
            catch (PasswordValidationDomainException ex)
            {
                return ServiceResult<RegisterResponse>.Failure(ex.Message, ErrorCode.ValidationError, "password");
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

            await _userRepository.AddAsync(user, cancellationToken);

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
            return ServiceResult<RegisterResponse>.Failure("An error occurred during registration", ErrorCode.InternalError);
        }
    }

    public async Task<ServiceResult<RefreshResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return ServiceResult<RefreshResponse>.Failure("Refresh token is required", ErrorCode.ValidationError, "refreshToken");
            }

            // Find refresh token in database
            var tokenEntity = await _userRepository.GetRefreshTokenAsync(refreshToken, cancellationToken);
            if (tokenEntity == null)
            {
                _logger.LogWarning("Refresh token not found: {Token}", refreshToken);
                return ServiceResult<RefreshResponse>.Failure("Invalid refresh token", ErrorCode.Unauthorized);
            }

            // Check if token is expired
            if (tokenEntity.ExpiresAt < DateTime.UtcNow)
            {
                await _userRepository.RevokeRefreshTokenAsync(refreshToken, cancellationToken);
                return ServiceResult<RefreshResponse>.Failure("Refresh token has expired", ErrorCode.Unauthorized);
            }

            // Check if token is revoked
            if (tokenEntity.RevokedAt.HasValue)
            {
                _logger.LogWarning("Refresh token revoked for user: {UserId}", tokenEntity.UserId);
                return ServiceResult<RefreshResponse>.Failure("Refresh token has been revoked", ErrorCode.Unauthorized);
            }

            // Get user
            var user = tokenEntity.User;

            if (!user.IsActive)
            {
                _logger.LogWarning("Refresh token used for inactive user: {UserId}", user.Id);
                await _userRepository.RevokeAllUserRefreshTokensAsync(user.Id, cancellationToken);
                return ServiceResult<RefreshResponse>.Failure("User account is deactivated", ErrorCode.Forbidden);
            }

            // Generate new tokens
            var newAccessToken = _jwtService.GenerateAccessToken(user);
            var newRefreshToken = await IssueRefreshTokenAsync(user, cancellationToken);

            // Revoke old refresh token
            await _userRepository.RevokeRefreshTokenAsync(refreshToken, cancellationToken);

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
            return ServiceResult<RefreshResponse>.Failure("An error occurred during token refresh", ErrorCode.InternalError);
        }
    }

    public async Task<ServiceResult<LogoutResponse>> RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return ServiceResult<LogoutResponse>.Failure("Refresh token is required", ErrorCode.ValidationError, "refreshToken");
            }

            await _userRepository.RevokeRefreshTokenAsync(refreshToken, cancellationToken);

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
            return ServiceResult<LogoutResponse>.Failure("An error occurred during logout", ErrorCode.InternalError);
        }
    }

    public async Task<ServiceResult<LogoutResponse>> RevokeAllUserRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return ServiceResult<LogoutResponse>.Failure("User not found", ErrorCode.NotFound);
            }

            await _userRepository.RevokeAllUserRefreshTokensAsync(userId, cancellationToken);

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
            return ServiceResult<LogoutResponse>.Failure("An error occurred while revoking refresh tokens", ErrorCode.InternalError);
        }
    }

    private async Task<string> IssueRefreshTokenAsync(User user, CancellationToken cancellationToken = default)
    {
        var refreshToken = _jwtService.GenerateRefreshToken(user);
        var tokenEntity = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddRefreshTokenAsync(tokenEntity, cancellationToken);
        return refreshToken;
    }

    public async Task<ServiceResult<ForgotPasswordResponse>> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return ServiceResult<ForgotPasswordResponse>.Failure("Email is required", ErrorCode.ValidationError, "email");
            }

            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
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

            await _userRepository.UpdateAsync(user, cancellationToken);

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
            return ServiceResult<ForgotPasswordResponse>.Failure("An error occurred while processing the request", ErrorCode.InternalError);
        }
    }

    public async Task<ServiceResult<ResetPasswordResponse>> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword))
            {
                return ServiceResult<ResetPasswordResponse>.Failure("Token and new password are required", ErrorCode.ValidationError);
            }

            // Validate new password
            try
            {
                _passwordValidator.Validate(newPassword);
            }
            catch (PasswordValidationDomainException ex)
            {
                return ServiceResult<ResetPasswordResponse>.Failure(ex.Message, ErrorCode.ValidationError, "newPassword");
            }
            // Find user by reset token
            var user = await _userRepository.GetByPasswordResetTokenAsync(token, cancellationToken);
            if (user == null)
            {
                return ServiceResult<ResetPasswordResponse>.Failure("Invalid or expired reset token", ErrorCode.Unauthorized);
            }

            // Check if token is expired
            if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
            {
                _logger.LogWarning("Password reset attempted with expired token for user: {UserId}", user.Id);
                return ServiceResult<ResetPasswordResponse>.Failure("Reset token has expired", ErrorCode.Unauthorized);
            }

            // Hash new password
            var passwordHash = _passwordHasher.HashPassword(newPassword);

            // Update user
            user.PasswordHash = passwordHash;
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);

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
            return ServiceResult<ResetPasswordResponse>.Failure("An error occurred while resetting the password", ErrorCode.InternalError);
        }
    }

    private static string GeneratePasswordResetToken()
    {
        return Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }



    public async Task<ServiceResult<UserOrganizationsResponse>> GetUserOrganizationsAsync(Guid userId, UserOrganizationsRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return ServiceResult<UserOrganizationsResponse>.Failure("User not found", ErrorCode.NotFound);
            }

            // Validate pagination parameters
            if (request.PageNumber < 1) request.PageNumber = 1;
            if (request.PageSize < 1 || request.PageSize > 100) request.PageSize = 10;

            // Create pagination request for repository
            var paginationRequest = new PaginationRequest
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                SortDescending = request.SortDescending
            };

            // Get paginated results from repository
            var paginatedResult = await _userOrganizationRepository.GetPaginatedUserOrganizationsAsync(
                userId,
                paginationRequest,
                request.IsActive,
                request.Role,
                cancellationToken);

            // Map to DTOs
            var organizations = paginatedResult.Items
                .Where(m => m.Organization.IsActive) // Additional filter for active organizations
                .Select(m => new OrganizationSummaryDto
                {
                    Id = m.Organization.Id,
                    Name = m.Organization.Name,
                    Slug = m.Organization.Slug,
                    Role = m.Role.ToString(),
                    IsActive = m.IsActive
                })
                .ToList();

            var response = new UserOrganizationsResponse
            {
                Organizations = organizations,
                TotalCount = paginatedResult.TotalCount,
                PageNumber = paginatedResult.PageNumber,
                PageSize = paginatedResult.PageSize,
                TotalPages = paginatedResult.TotalPages,
                HasNextPage = paginatedResult.HasNextPage,
                HasPreviousPage = paginatedResult.HasPreviousPage
            };

            return ServiceResult<UserOrganizationsResponse>.Success(response, "User organizations retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organizations for user {UserId}", userId);
            return ServiceResult<UserOrganizationsResponse>.Failure("An error occurred while retrieving user organizations", ErrorCode.InternalError);
        }
    }
}
