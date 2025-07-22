using SermonTranscription.Domain.Entities;

namespace SermonTranscription.Domain.Interfaces;

/// <summary>
/// JWT service interface for token generation and validation
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generate a JWT access token for a user (contains only user identity, no tenant info)
    /// </summary>
    /// <param name="user">The user to generate a token for</param>
    /// <returns>The generated JWT token</returns>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Generate a refresh token for a user
    /// </summary>
    /// <param name="user">The user to generate a refresh token for</param>
    /// <returns>The generated refresh token</returns>
    string GenerateRefreshToken(User user);

    /// <summary>
    /// Validate a JWT token and extract user information
    /// </summary>
    /// <param name="token">The JWT token to validate</param>
    /// <returns>User information if token is valid, null otherwise</returns>
    Task<JwtUserInfo?> ValidateTokenAsync(string token);

    /// <summary>
    /// Extract user ID from a JWT token without full validation
    /// </summary>
    /// <param name="token">The JWT token</param>
    /// <returns>User ID if token is valid, null otherwise</returns>
    Guid? GetUserIdFromToken(string token);
}

/// <summary>
/// User information extracted from JWT token
/// </summary>
public class JwtUserInfo
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
