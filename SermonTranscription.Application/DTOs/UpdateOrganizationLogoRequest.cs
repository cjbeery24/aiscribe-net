namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for organization logo update
/// </summary>
public class UpdateOrganizationLogoRequest
{
    public string LogoUrl { get; set; } = string.Empty;
}
