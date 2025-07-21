using System.ComponentModel.DataAnnotations;

namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for updating organization user role
/// </summary>
public class UpdateOrganizationUserRoleRequest
{
    [Required(ErrorMessage = "Role is required")]
    public string Role { get; set; } = string.Empty;
}
