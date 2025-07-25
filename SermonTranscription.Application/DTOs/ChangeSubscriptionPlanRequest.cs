using SermonTranscription.Domain.Enums;

namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for changing a subscription plan
/// </summary>
public class ChangeSubscriptionPlanRequest
{
    public SubscriptionPlan NewPlan { get; set; }
}
