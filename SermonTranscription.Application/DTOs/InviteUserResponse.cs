namespace SermonTranscription.Application.DTOs;

/// <summary>
/// Response DTO for user invitation
/// </summary>
public class InviteUserResponse
{
    /// <summary>
    /// Whether the invitation was sent successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Email address of the invited user
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Invitation token (for testing/debugging purposes)
    /// </summary>
    public string? InvitationToken { get; set; }
}
