using SermonTranscription.Domain.Enums;
using System.Text.Json.Serialization;

namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for creating a new subscription
/// </summary>
public class CreateSubscriptionRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter<SubscriptionPlan>))]
    public SubscriptionPlan Plan { get; set; }
}
