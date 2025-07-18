using System.ComponentModel.DataAnnotations;
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
    public int MaxUsers { get; set; } = 5;
    public int MaxTranscriptionHours { get; set; } = 10;
    public bool CanExportTranscriptions { get; set; } = false;
    public bool HasRealtimeTranscription { get; set; } = true;
    
    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<TranscriptionSession> TranscriptionSessions { get; set; } = new List<TranscriptionSession>();
    public ICollection<Transcription> Transcriptions { get; set; } = new List<Transcription>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    
    // Computed properties
    public string DisplayName => !string.IsNullOrEmpty(Name) ? Name : "Unnamed Organization";
    
    // Domain methods
    public bool CanAddUser()
    {
        return Users.Count < MaxUsers;
    }
    
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
        return Users.Count(u => u.IsActive);
    }
    
    public bool CanAddMoreUsers()
    {
        return GetActiveUserCount() < MaxUsers;
    }
    
    public void UpdateSubscriptionLimits(int maxUsers, int maxTranscriptionHours, bool canExport, bool hasRealtime)
    {
        MaxUsers = maxUsers;
        MaxTranscriptionHours = maxTranscriptionHours;
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
    
    public void ValidateCanAddUser()
    {
        ValidateIsActive();
        
        if (!CanAddMoreUsers())
        {
            throw new OrganizationUserLimitException(
                $"Organization {Name} has reached the maximum number of users ({MaxUsers}).");
        }
    }
    
    public void ValidateHasActiveSubscription()
    {
        if (!HasActiveSubscription())
        {
            throw new OrganizationSubscriptionLimitException($"Organization {Name} does not have an active subscription.");
        }
    }
    
    public void ValidateCanCreateTranscription()
    {
        ValidateIsActive();
        ValidateHasActiveSubscription();
        
        if (!CanCreateTranscription())
        {
            throw new OrganizationFeatureNotAvailableException(
                $"Organization {Name} does not have real-time transcription enabled.");
        }
    }
    
    public void ValidateCanExportTranscriptions()
    {
        ValidateIsActive();
        ValidateHasActiveSubscription();
        
        if (!CanExportTranscriptionsEnabled())
        {
            throw new OrganizationFeatureNotAvailableException(
                $"Organization {Name} does not have transcription export enabled.");
        }
    }
    
    public void ValidateHasRealtimeTranscription()
    {
        ValidateIsActive();
        ValidateHasActiveSubscription();
        
        if (!HasRealtimeTranscriptionEnabled())
        {
            throw new OrganizationFeatureNotAvailableException(
                $"Organization {Name} does not have real-time transcription enabled.");
        }
    }
} 