namespace SermonTranscription.Application.DTOs;

/// <summary>
/// Authentication user information DTO
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
