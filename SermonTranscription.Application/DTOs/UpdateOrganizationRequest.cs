namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for updating an existing organization
/// </summary>
public class UpdateOrganizationRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ContactEmail { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? WebsiteUrl { get; set; }
    public bool? IsActive { get; set; }
}
