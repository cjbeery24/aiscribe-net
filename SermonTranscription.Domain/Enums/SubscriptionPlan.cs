namespace SermonTranscription.Domain.Enums;

/// <summary>
/// Defines the available subscription plans
/// </summary>
public enum SubscriptionPlan
{
    /// <summary>
    /// Free tier with limited features
    /// </summary>
    Free = 0,
    
    /// <summary>
    /// Basic tier for small organizations
    /// </summary>
    Basic = 1,
    
    /// <summary>
    /// Professional tier for growing organizations
    /// </summary>
    Professional = 2,
    
    /// <summary>
    /// Enterprise tier for large organizations
    /// </summary>
    Enterprise = 3
} 