namespace SermonTranscription.Domain.Enums;

/// <summary>
/// Defines the available subscription plans
/// </summary>
public enum SubscriptionPlan
{
    /// <summary>
    /// Basic tier for small organizations
    /// </summary>
    Basic = 0,

    /// <summary>
    /// Professional tier for growing organizations
    /// </summary>
    Professional = 1,

    /// <summary>
    /// Enterprise tier for large organizations
    /// </summary>
    Enterprise = 2
}
