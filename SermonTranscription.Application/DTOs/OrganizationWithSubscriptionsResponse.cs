namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for organization response that includes subscription information
/// </summary>
public class OrganizationWithSubscriptionsResponse : OrganizationResponse
{
    /// <summary>
    /// List of subscriptions for the organization
    /// </summary>
    public List<SubscriptionResponse> Subscriptions { get; set; } = new();

    /// <summary>
    /// Current active subscription (if any)
    /// </summary>
    public SubscriptionResponse? ActiveSubscription { get; set; }
}
