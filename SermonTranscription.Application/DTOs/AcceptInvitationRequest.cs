namespace SermonTranscription.Application.DTOs;

/// <summary>
/// Request DTO for accepting an organization invitation
/// </summary>
public class AcceptInvitationRequest
{
    /// <summary>
    /// Invitation token received via email
    /// </summary>
    public string InvitationToken { get; set; } = string.Empty;

    /// <summary>
    /// Password for the new user account (if user doesn't exist)
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
