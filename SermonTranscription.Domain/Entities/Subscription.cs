using System.ComponentModel.DataAnnotations;

namespace SermonTranscription.Domain.Entities;

public enum SubscriptionPlan
{
    Free,
    Basic,
    Professional,
    Enterprise
}

public enum SubscriptionStatus
{
    Active,
    PastDue,
    Cancelled,
    Suspended,
    Expired
}

public class Subscription
{
    public Guid Id { get; set; }
    
    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Free;
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? CancelledAt { get; set; }
    
    // Billing
    public decimal MonthlyPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime? NextBillingDate { get; set; }
    public DateTime? LastBillingDate { get; set; }
    
    // Stripe integration
    public string? StripeSubscriptionId { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? StripePriceId { get; set; }
    
    // Plan limits
    public int MaxUsers { get; set; }
    public int MaxTranscriptionHours { get; set; }
    public bool CanExportTranscriptions { get; set; }
    public bool HasRealtimeTranscription { get; set; }
    public bool HasPrioritySupport { get; set; }
    
    // Usage tracking
    public int CurrentUsers { get; set; }
    public int TranscriptionHoursUsed { get; set; }
    public DateTime UsageResetDate { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    
    // Computed properties
    public bool IsActive => Status == SubscriptionStatus.Active;
    public bool IsExpired => EndDate.HasValue && EndDate.Value < DateTime.UtcNow;
    public bool IsCancelled => Status == SubscriptionStatus.Cancelled;
    
    public int RemainingUsers => Math.Max(0, MaxUsers - CurrentUsers);
    public int RemainingTranscriptionHours => Math.Max(0, MaxTranscriptionHours - TranscriptionHoursUsed);
    
    public decimal YearlyPrice => MonthlyPrice * 12;
    
    // Domain methods
    public void Cancel(DateTime? cancellationDate = null)
    {
        Status = SubscriptionStatus.Cancelled;
        CancelledAt = cancellationDate ?? DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Suspend()
    {
        Status = SubscriptionStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Reactivate()
    {
        if (Status == SubscriptionStatus.Cancelled || Status == SubscriptionStatus.Suspended)
        {
            Status = SubscriptionStatus.Active;
            CancelledAt = null;
            UpdatedAt = DateTime.UtcNow;
        }
    }
    
    public void UpgradePlan(SubscriptionPlan newPlan)
    {
        if (newPlan > Plan)
        {
            Plan = newPlan;
            UpdatePlanLimits();
            UpdatedAt = DateTime.UtcNow;
        }
    }
    
    public void DowngradePlan(SubscriptionPlan newPlan)
    {
        if (newPlan < Plan)
        {
            Plan = newPlan;
            UpdatePlanLimits();
            UpdatedAt = DateTime.UtcNow;
        }
    }
    
    public bool CanAddUser()
    {
        return IsActive && CurrentUsers < MaxUsers;
    }
    
    public bool CanUseTranscriptionHours(int hours)
    {
        return IsActive && (TranscriptionHoursUsed + hours) <= MaxTranscriptionHours;
    }
    
    public void UseTranscriptionHours(int hours)
    {
        if (!CanUseTranscriptionHours(hours))
            throw new InvalidOperationException("Insufficient transcription hours remaining");
        
        TranscriptionHoursUsed += hours;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void ResetUsage()
    {
        TranscriptionHoursUsed = 0;
        UsageResetDate = DateTime.UtcNow.AddMonths(1);
        UpdatedAt = DateTime.UtcNow;
    }
    
    private void UpdatePlanLimits()
    {
        switch (Plan)
        {
            case SubscriptionPlan.Free:
                MaxUsers = 2;
                MaxTranscriptionHours = 5;
                CanExportTranscriptions = false;
                HasRealtimeTranscription = true;
                HasPrioritySupport = false;
                MonthlyPrice = 0;
                break;
            
            case SubscriptionPlan.Basic:
                MaxUsers = 5;
                MaxTranscriptionHours = 25;
                CanExportTranscriptions = true;
                HasRealtimeTranscription = true;
                HasPrioritySupport = false;
                MonthlyPrice = 29.99m;
                break;
            
            case SubscriptionPlan.Professional:
                MaxUsers = 15;
                MaxTranscriptionHours = 100;
                CanExportTranscriptions = true;
                HasRealtimeTranscription = true;
                HasPrioritySupport = true;
                MonthlyPrice = 99.99m;
                break;
            
            case SubscriptionPlan.Enterprise:
                MaxUsers = 100;
                MaxTranscriptionHours = 500;
                CanExportTranscriptions = true;
                HasRealtimeTranscription = true;
                HasPrioritySupport = true;
                MonthlyPrice = 299.99m;
                break;
        }
    }
} 