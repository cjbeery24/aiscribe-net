using System.ComponentModel.DataAnnotations;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Exceptions;

namespace SermonTranscription.Domain.Entities;

public class Organization
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Slug { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(255)]
    [EmailAddress]
    public string? ContactEmail { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(50)]
    public string? State { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    public string? LogoUrl { get; set; }
    public string? WebsiteUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public bool IsActive { get; set; } = true;

    // Subscription settings
    public int MaxTranscriptionMinutes { get; set; } = 600;
    public bool CanExportTranscriptions { get; set; } = false;
    public bool HasRealtimeTranscription { get; set; } = true;

    // Navigation properties
    public ICollection<UserOrganization> UserOrganizations { get; set; } = new List<UserOrganization>();
    public ICollection<TranscriptionSession> TranscriptionSessions { get; set; } = new List<TranscriptionSession>();
    public ICollection<Transcription> Transcriptions { get; set; } = new List<Transcription>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();

    // Computed properties
    public string DisplayName => !string.IsNullOrEmpty(Name) ? Name : "Unnamed Organization";

    // Domain methods
    public bool CanCreateTranscription()
    {
        return IsActive && HasRealtimeTranscription;
    }

    public void UpdateSlug()
    {
        if (!string.IsNullOrEmpty(Name))
        {
            Slug = Name.ToLowerInvariant()
                      .Replace(" ", "-")
                      .Replace("'", "")
                      .Replace("\"", "");
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void UpdateContactInfo(string? email = null, string? phone = null)
    {
        if (!string.IsNullOrEmpty(email))
            ContactEmail = email;

        if (!string.IsNullOrEmpty(phone))
            PhoneNumber = phone;

        UpdatedAt = DateTime.UtcNow;
    }

    // Organization management methods
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasActiveSubscription()
    {
        return Subscriptions.Any(s => s.IsActive);
    }

    public Subscription? GetCurrentSubscription()
    {
        return Subscriptions.FirstOrDefault(s => s.IsActive);
    }

    public int GetActiveUserCount()
    {
        return UserOrganizations.Count(uo => uo.IsActive);
    }

    /// <summary>
    /// Get all active users in this organization
    /// </summary>
    public IEnumerable<User> GetActiveUsers()
    {
        return UserOrganizations
            .Where(uo => uo.IsActive)
            .Select(uo => uo.User);
    }

    /// <summary>
    /// Get all admin users in this organization
    /// </summary>
    public IEnumerable<User> GetAdminUsers()
    {
        return UserOrganizations
            .Where(uo => uo.IsActive && uo.Role == UserRole.OrganizationAdmin)
            .Select(uo => uo.User);
    }

    /// <summary>
    /// Check if a user is a member of this organization
    /// </summary>
    public bool HasUser(Guid userId)
    {
        return UserOrganizations.Any(uo => uo.UserId == userId && uo.IsActive);
    }

    /// <summary>
    /// Get a user's membership in this organization
    /// </summary>
    public UserOrganization? GetUserMembership(Guid userId)
    {
        return UserOrganizations.FirstOrDefault(uo => uo.UserId == userId && uo.IsActive);
    }

    public void UpdateSubscriptionLimits(int maxTranscriptionMinutes, bool canExport, bool hasRealtime)
    {
        MaxTranscriptionMinutes = maxTranscriptionMinutes;
        CanExportTranscriptions = canExport;
        HasRealtimeTranscription = hasRealtime;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasRealtimeTranscriptionEnabled()
    {
        return IsActive && HasRealtimeTranscription && HasActiveSubscription();
    }

    public bool CanExportTranscriptionsEnabled()
    {
        return IsActive && CanExportTranscriptions && HasActiveSubscription();
    }

    public string GetFullAddress()
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(Address))
            parts.Add(Address);

        if (!string.IsNullOrEmpty(City))
            parts.Add(City);

        if (!string.IsNullOrEmpty(State))
            parts.Add(State);

        if (!string.IsNullOrEmpty(PostalCode))
            parts.Add(PostalCode);

        if (!string.IsNullOrEmpty(Country))
            parts.Add(Country);

        return string.Join(", ", parts);
    }

    public bool HasCompleteContactInfo()
    {
        return !string.IsNullOrEmpty(ContactEmail) && !string.IsNullOrEmpty(PhoneNumber);
    }

    public void UpdateLogo(string logoUrl)
    {
        LogoUrl = logoUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateWebsite(string websiteUrl)
    {
        WebsiteUrl = websiteUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    // Validation methods
    public void ValidateIsActive()
    {
        if (!IsActive)
        {
            throw new OrganizationInactiveException($"Organization {Name} is not active.");
        }
    }

    public void ValidateCanCreateTranscription()
    {
        ValidateIsActive();

        if (!CanCreateTranscription())
        {
            throw new OrganizationFeatureNotAvailableException(
                $"Organization {Name} does not have real-time transcription enabled.");
        }
    }

    public void ValidateCanExportTranscriptions()
    {
        ValidateIsActive();

        if (!CanExportTranscriptionsEnabled())
        {
            throw new OrganizationFeatureNotAvailableException(
                $"Organization {Name} does not have transcription export enabled.");
        }
    }

    public void ValidateHasRealtimeTranscription()
    {
        ValidateIsActive();

        if (!HasRealtimeTranscriptionEnabled())
        {
            throw new OrganizationFeatureNotAvailableException(
                $"Organization {Name} does not have real-time transcription enabled.");
        }
    }
}
