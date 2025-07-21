namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for organization user response
/// </summary>
public class OrganizationUserResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsEmailVerified { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool CanManageUsers { get; set; }
    public bool CanViewTranscriptions { get; set; }
    public bool CanExportTranscriptions { get; set; }
    public bool CanManageTranscriptions { get; set; }
}
