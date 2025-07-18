namespace SermonTranscription.Domain.Enums;

/// <summary>
/// Defines the status of a subscription
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>
    /// Subscription is active and billing normally
    /// </summary>
    Active = 0,
    
    /// <summary>
    /// Payment is past due but subscription is still active
    /// </summary>
    PastDue = 1,
    
    /// <summary>
    /// Subscription has been cancelled
    /// </summary>
    Cancelled = 2,
    
    /// <summary>
    /// Subscription is suspended due to payment issues
    /// </summary>
    Suspended = 3,
    
    /// <summary>
    /// Subscription has expired
    /// </summary>
    Expired = 4
} 