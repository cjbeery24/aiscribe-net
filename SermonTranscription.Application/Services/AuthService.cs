using Microsoft.Extensions.Logging;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Interfaces;
using SermonTranscription.Domain.Exceptions;

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

    public AuthService(
        IUserRepository userRepository,
        IUserOrganizationRepository userOrganizationRepository,
        IJwtService jwtService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _userOrganizationRepository = userOrganizationRepository;
        _jwtService = jwtService;
        _logger = logger;
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
            if (!VerifyPassword(password, user.PasswordHash))
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

            // Generate tokens
            var accessToken = _jwtService.GenerateAccessToken(user, primaryMembership.OrganizationId, primaryMembership.Role.ToString());
            var refreshToken = _jwtService.GenerateRefreshToken(user);

            // Update user's last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Successful login for user {UserId}", user.Id);

            return AuthResult.Success(accessToken, refreshToken, new AuthUserInfo
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                OrganizationId = primaryMembership.OrganizationId,
                Role = primaryMembership.Role.ToString()
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

            if (request.Password.Length < 8)
            {
                return AuthResult.Failure("Password must be at least 8 characters long");
            }

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
                PasswordHash = HashPassword(request.Password),
                IsEmailVerified = false, // Will be verified via email
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);

            _logger.LogInformation("New user registered: {UserId}", user.Id);

            return AuthResult.Success("User registered successfully. Please check your email to verify your account.");
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
            // In a real implementation, you would validate the refresh token against a database
            // For now, we'll return an error indicating this needs to be implemented
            _logger.LogWarning("Refresh token functionality not yet implemented");
            return AuthResult.Failure("Refresh token functionality not yet implemented");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return AuthResult.Failure("An error occurred during token refresh");
        }
    }

    public async Task<AuthResult> ValidateTokenAsync(string token)
    {
        try
        {
            var userInfo = _jwtService.ValidateToken(token);
            if (userInfo == null)
            {
                return AuthResult.Failure("Invalid or expired token");
            }

            // Check if user still exists and is active
            var user = await _userRepository.GetByIdAsync(userInfo.UserId);
            if (user == null || !user.IsActive)
            {
                return AuthResult.Failure("User account is no longer valid");
            }

            // Check if user still has membership in the organization
            var memberships = await _userOrganizationRepository.GetUserOrganizationsAsync(userInfo.UserId);
            var membership = memberships.FirstOrDefault(m => m.OrganizationId == userInfo.OrganizationId && m.IsActive);
            if (membership == null)
            {
                return AuthResult.Failure("User is no longer a member of this organization");
            }

            return AuthResult.Success(new AuthUserInfo
            {
                UserId = userInfo.UserId,
                Email = userInfo.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                OrganizationId = userInfo.OrganizationId,
                Role = userInfo.Role
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return AuthResult.Failure("An error occurred during token validation");
        }
    }

    private static string HashPassword(string password)
    {
        // In a real implementation, use BCrypt.Net-Next or similar
        // For now, return a placeholder hash
        return "placeholder_hash_" + password; // TODO: Implement proper password hashing
    }

    private static bool VerifyPassword(string password, string hash)
    {
        // In a real implementation, use BCrypt.Net-Next or similar
        // For now, return a placeholder verification
        return hash == "placeholder_hash_" + password; // TODO: Implement proper password verification
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
    Task<AuthResult> ValidateTokenAsync(string token);
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

/// <summary>
/// Authentication user information
/// </summary>
public class AuthUserInfo
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// User registration request
/// </summary>
public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
} 