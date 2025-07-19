using SermonTranscription.Domain.Entities;

namespace SermonTranscription.Domain.Interfaces;

/// <summary>
/// JWT service interface for token generation and validation
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generate a JWT access token for a user
    /// </summary>
    /// <param name="user">The user to generate a token for</param>
    /// <param name="organizationId">The organization context for the token</param>
    /// <param name="role">The user's role in the organization</param>
    /// <returns>The generated JWT token</returns>
    string GenerateAccessToken(User user, Guid organizationId, string role);

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
    JwtUserInfo? ValidateToken(string token);

    /// <summary>
    /// Extract user ID from a JWT token without full validation
    /// </summary>
    /// <param name="token">The JWT token</param>
    /// <returns>User ID if token is valid, null otherwise</returns>
    Guid? GetUserIdFromToken(string token);

    /// <summary>
    /// Extract organization ID from a JWT token
    /// </summary>
    /// <param name="token">The JWT token</param>
    /// <returns>Organization ID if present in token, null otherwise</returns>
    Guid? GetOrganizationIdFromToken(string token);
}

/// <summary>
/// User information extracted from JWT token
/// </summary>
public class JwtUserInfo
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public DateTime ExpiresAt { get; set; }
} 