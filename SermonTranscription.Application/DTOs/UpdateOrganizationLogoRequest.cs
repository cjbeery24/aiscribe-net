using System.ComponentModel.DataAnnotations;

namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for organization logo update
/// </summary>
public class UpdateOrganizationLogoRequest
{
    [Required(ErrorMessage = "Logo URL is required")]
    [Url(ErrorMessage = "Invalid logo URL")]
    public string LogoUrl { get; set; } = string.Empty;
}
