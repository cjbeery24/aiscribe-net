using System.ComponentModel.DataAnnotations;

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
} 