using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Common;
using SermonTranscription.Domain.Enums;

namespace SermonTranscription.Application.Interfaces;

/// <summary>
/// Service interface for managing subscription plans and tier-based feature access
/// </summary>
public interface ISubscriptionService
{
    /// <summary>
    /// Get the current active subscription for an organization
    /// </summary>
    Task<ServiceResult<SubscriptionResponse?>> GetCurrentSubscriptionAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all subscriptions for an organization
    /// </summary>
    Task<ServiceResult<IEnumerable<SubscriptionResponse>>> GetOrganizationSubscriptionsAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paginated subscription history for an organization
    /// </summary>
    Task<ServiceResult<SubscriptionHistoryResponse>> GetSubscriptionHistoryAsync(Guid organizationId, SubscriptionHistoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new subscription for an organization
    /// </summary>
    Task<ServiceResult<SubscriptionResponse>> CreateSubscriptionAsync(Guid organizationId, SubscriptionPlan plan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upgrade or downgrade a subscription plan
    /// </summary>
    Task<ServiceResult<SubscriptionResponse>> ChangeSubscriptionPlanAsync(Guid subscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a subscription
    /// </summary>
    Task<ServiceResult<SubscriptionResponse>> CancelSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reactivate a cancelled or suspended subscription
    /// </summary>
    Task<ServiceResult<SubscriptionResponse>> ReactivateSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Track usage of transcription minutes
    /// </summary>
    Task<ServiceResult<SubscriptionResponse>> TrackTranscriptionUsageAsync(Guid subscriptionId, int minutesUsed, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset monthly usage for a subscription
    /// </summary>
    Task<ServiceResult<SubscriptionResponse>> ResetMonthlyUsageAsync(Guid subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if an organization can use transcription minutes based on subscription limits
    /// </summary>
    Task<ServiceResult<bool>> CanUseTranscriptionMinutesAsync(Guid organizationId, int minutes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get subscription usage analytics for an organization
    /// </summary>
    Task<ServiceResult<SubscriptionUsageResponse>> GetUsageAnalyticsAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available subscription plans with their features and pricing
    /// </summary>
    Task<ServiceResult<IEnumerable<SubscriptionPlanResponse>>> GetAvailablePlansAsync(CancellationToken cancellationToken = default);
}
