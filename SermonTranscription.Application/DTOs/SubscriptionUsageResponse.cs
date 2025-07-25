using SermonTranscription.Domain.Enums;

namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for subscription usage analytics
/// </summary>
public class SubscriptionUsageResponse
{
    public Guid OrganizationId { get; set; }
    public SubscriptionPlan CurrentPlan { get; set; }
    public string PlanName { get; set; } = string.Empty;

    // Usage limits
    public int MonthlyLimit { get; set; }
    public int MinutesUsed { get; set; }
    public int MinutesRemaining { get; set; }
    public int TotalUsage { get; set; }
    public decimal UsagePercentage { get; set; }

    // Usage tracking
    public DateTime UsageResetDate { get; set; }
    public bool IsNearLimit { get; set; }

    // Computed properties
    public bool IsOverLimit => MinutesUsed > MonthlyLimit;
    public bool IsAtLimit => MinutesUsed >= MonthlyLimit;
    public int DaysUntilReset => (UsageResetDate - DateTime.UtcNow).Days;
}
