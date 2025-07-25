namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for updating organization user role
/// </summary>
public class UpdateOrganizationUserRoleRequest
{
    public string Role { get; set; } = string.Empty;
}
