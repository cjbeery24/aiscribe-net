namespace SermonTranscription.Application.DTOs;

/// <summary>
/// Response DTO for accepting an organization invitation
/// </summary>
public class AcceptInvitationResponse
{
    /// <summary>
    /// Whether the invitation was accepted successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Organization name the user was invited to
    /// </summary>
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>
    /// User's role in the organization
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// JWT access token for the newly authenticated user
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// JWT refresh token for the newly authenticated user
    /// </summary>
    public string? RefreshToken { get; set; }
}
