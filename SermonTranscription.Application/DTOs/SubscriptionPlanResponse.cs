using SermonTranscription.Domain.Enums;

namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for subscription plan information
/// </summary>
public class SubscriptionPlanResponse
{
    public SubscriptionPlan Plan { get; set; }
    public string PlanName { get; set; } = string.Empty;

    // Pricing
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }

    // Plan limits
    public int MaxTranscriptionMinutes { get; set; }
    public bool CanExportTranscriptions { get; set; }
    public bool HasRealtimeTranscription { get; set; }
    public bool HasPrioritySupport { get; set; }

    // Features list
    public List<string> Features { get; set; } = new();

    // Computed properties
    public decimal YearlySavings => (MonthlyPrice * 12) - YearlyPrice;
    public bool IsPopular => Plan == SubscriptionPlan.Professional;
    public bool IsRecommended => Plan == SubscriptionPlan.Basic;
}
