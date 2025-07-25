using SermonTranscription.Domain.Enums;

namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for subscription response
/// </summary>
public class SubscriptionResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public SubscriptionPlan Plan { get; set; }
    public SubscriptionStatus Status { get; set; }

    // Dates
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Billing
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    public string Currency { get; set; } = string.Empty;
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

    // Computed properties
    public bool IsActive { get; set; }
    public bool IsExpired { get; set; }
    public bool IsCancelled { get; set; }
    public int RemainingTranscriptionMinutes { get; set; }

    // Plan name
    public string PlanName { get; set; } = string.Empty;
}
