namespace SermonTranscription.Application.DTOs;

/// <summary>
/// Request DTO for inviting a user to an organization
/// </summary>
public class InviteUserRequest
{
    /// <summary>
    /// Email address of the user to invite
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// First name of the user to invite
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name of the user to invite
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Role to assign to the user in the organization
    /// </summary>
    public string Role { get; set; } = "OrganizationUser";

    /// <summary>
    /// Optional message to include in the invitation email
    /// </summary>
    public string? Message { get; set; }
}
