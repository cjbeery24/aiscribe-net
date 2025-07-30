using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Common;

namespace SermonTranscription.Domain.Interfaces;

/// <summary>
/// Repository interface for Subscription entity with subscription-specific operations
/// </summary>
public interface ISubscriptionRepository : IBaseRepository<Subscription>
{
    // Organization-specific queries
    Task<Subscription?> GetActiveByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Subscription>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<Subscription?> GetCurrentSubscriptionAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<PaginatedResult<Subscription>> GetPaginatedByOrganizationAsync(Guid organizationId, PaginationRequest request, string? status = null, string? plan = null, CancellationToken cancellationToken = default);

    // Status-based queries
    Task<IEnumerable<Subscription>> GetByStatusAsync(SubscriptionStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Subscription>> GetActiveSubscriptionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Subscription>> GetExpiredSubscriptionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Subscription>> GetExpiringSubscriptionsAsync(DateTime beforeDate, CancellationToken cancellationToken = default);

    // Plan-based queries
    Task<IEnumerable<Subscription>> GetByPlanAsync(SubscriptionPlan plan, CancellationToken cancellationToken = default);
    Task<int> GetActiveSubscriptionCountByPlanAsync(SubscriptionPlan plan, CancellationToken cancellationToken = default);

    // Billing and payment queries
    Task<Subscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Subscription>> GetSubscriptionsDueForBillingAsync(DateTime dueDate, CancellationToken cancellationToken = default);

    // Usage tracking
    Task<int> GetTotalUsageMinutesAsync(Guid organizationId, DateTime? fromDate = null, CancellationToken cancellationToken = default);
    Task UpdateUsageAsync(Guid subscriptionId, int minutesUsed, CancellationToken cancellationToken = default);
    Task ResetMonthlyUsageAsync(Guid subscriptionId, CancellationToken cancellationToken = default);

    // Analytics
    Task<IDictionary<SubscriptionPlan, int>> GetSubscriptionDistributionAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetMonthlyRecurringRevenueAsync(CancellationToken cancellationToken = default);
}
