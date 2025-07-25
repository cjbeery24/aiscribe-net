using System.ComponentModel.DataAnnotations;
using SermonTranscription.Domain.Enums;

namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for creating a new subscription
/// </summary>
public class CreateSubscriptionRequest
{
    [Required]
    public SubscriptionPlan Plan { get; set; }

    [Required]
    public Guid OrganizationId { get; set; }
}
