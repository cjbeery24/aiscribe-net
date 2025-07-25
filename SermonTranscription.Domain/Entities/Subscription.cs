using System.ComponentModel.DataAnnotations;
using SermonTranscription.Domain.Enums;

namespace SermonTranscription.Domain.Entities;

public class Subscription
{
    public Guid Id { get; set; }

    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Basic;
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
    public int MaxTranscriptionMinutes { get; set; }
    public bool CanExportTranscriptions { get; set; }
    public bool HasRealtimeTranscription { get; set; }
    public bool HasPrioritySupport { get; set; }

    // Usage tracking
    public int CurrentUsers { get; set; }
    public int TranscriptionMinutesUsed { get; set; }
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

    // Removed RemainingUsers and all MaxUsers logic
    public int RemainingTranscriptionMinutes => Math.Max(0, MaxTranscriptionMinutes - TranscriptionMinutesUsed);

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

    public void ChangePlan(SubscriptionPlan newPlan)
    {
        if (newPlan != Plan)
        {
            Plan = newPlan;
            UpdatePlanLimits();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public bool CanUseTranscriptionMinutes(int minutes)
    {
        return IsActive && (TranscriptionMinutesUsed + minutes) <= MaxTranscriptionMinutes;
    }

    public void UseTranscriptionMinutes(int minutes)
    {
        if (!CanUseTranscriptionMinutes(minutes))
            throw new InvalidOperationException("Insufficient transcription minutes remaining");

        TranscriptionMinutesUsed += minutes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ResetUsage()
    {
        TranscriptionMinutesUsed = 0;
        UsageResetDate = DateTime.UtcNow.AddMonths(1);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePlanLimits()
    {
        switch (Plan)
        {
            case SubscriptionPlan.Basic:
                MaxTranscriptionMinutes = 360;
                CanExportTranscriptions = true;
                HasRealtimeTranscription = true;
                HasPrioritySupport = false;
                MonthlyPrice = 48.00m;
                break;
            case SubscriptionPlan.Professional:
                MaxTranscriptionMinutes = 600;
                CanExportTranscriptions = true;
                HasRealtimeTranscription = true;
                HasPrioritySupport = true;
                MonthlyPrice = 80.00m;
                break;
            case SubscriptionPlan.Enterprise:
                MaxTranscriptionMinutes = 840;
                CanExportTranscriptions = true;
                HasRealtimeTranscription = true;
                HasPrioritySupport = true;
                MonthlyPrice = 112.00m;
                break;
        }
    }
}
