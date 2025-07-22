namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for organization summary (used in lists)
/// </summary>
public class OrganizationSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public int ActiveUserCount { get; set; }
    public int MaxUsers { get; set; }
    public bool HasActiveSubscription { get; set; }
    public string? Role { get; set; } // User's role in this organization (null if not a member)
}
