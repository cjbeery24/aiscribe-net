using SermonTranscription.Domain.Enums;

namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for creating a new subscription
/// </summary>
public class CreateSubscriptionRequest
{
    public SubscriptionPlan Plan { get; set; }
    public Guid OrganizationId { get; set; }
}
