namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for organization response
/// </summary>
public class OrganizationResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? ContactEmail { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? LogoUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }

    // Subscription settings
    public int MaxUsers { get; set; }
    public int MaxTranscriptionHours { get; set; }
    public bool CanExportTranscriptions { get; set; }
    public bool HasRealtimeTranscription { get; set; }

    // Computed properties
    public string DisplayName { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public bool HasCompleteContactInfo { get; set; }
    public bool HasActiveSubscription { get; set; }
    public int ActiveUserCount { get; set; }
    public bool CanAddMoreUsers { get; set; }
    public bool CanCreateTranscription { get; set; }
    public bool HasRealtimeTranscriptionEnabled { get; set; }
    public bool CanExportTranscriptionsEnabled { get; set; }
}
