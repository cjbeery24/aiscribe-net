using Microsoft.EntityFrameworkCore;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Interfaces;
using SermonTranscription.Infrastructure.Data;

namespace SermonTranscription.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Subscription entity
/// </summary>
public class SubscriptionRepository : BaseRepository<Subscription>, ISubscriptionRepository
{
    public SubscriptionRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Subscription?> GetActiveByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .Include(s => s.Organization)
            .Where(s => s.OrganizationId == organizationId && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<Subscription>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .Include(s => s.Organization)
            .Where(s => s.OrganizationId == organizationId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Subscription?> GetCurrentSubscriptionAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await GetActiveByOrganizationAsync(organizationId, cancellationToken);
    }

    public async Task<IEnumerable<Subscription>> GetByStatusAsync(SubscriptionStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .Include(s => s.Organization)
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Subscription>> GetActiveSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        return await GetByStatusAsync(SubscriptionStatus.Active, cancellationToken);
    }

    public async Task<IEnumerable<Subscription>> GetExpiredSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .Include(s => s.Organization)
            .Where(s => s.EndDate.HasValue && s.EndDate.Value < DateTime.UtcNow)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Subscription>> GetExpiringSubscriptionsAsync(DateTime beforeDate, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .Include(s => s.Organization)
            .Where(s => s.NextBillingDate.HasValue && s.NextBillingDate.Value <= beforeDate && s.Status == SubscriptionStatus.Active)
            .OrderBy(s => s.NextBillingDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Subscription>> GetByPlanAsync(SubscriptionPlan plan, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .Include(s => s.Organization)
            .Where(s => s.Plan == plan)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetActiveSubscriptionCountByPlanAsync(SubscriptionPlan plan, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .CountAsync(s => s.Plan == plan && s.Status == SubscriptionStatus.Active, cancellationToken);
    }

    public async Task<Subscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .Include(s => s.Organization)
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId, cancellationToken);
    }

    public async Task<IEnumerable<Subscription>> GetSubscriptionsDueForBillingAsync(DateTime dueDate, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .Include(s => s.Organization)
            .Where(s => s.NextBillingDate.HasValue && s.NextBillingDate.Value <= dueDate && s.Status == SubscriptionStatus.Active)
            .OrderBy(s => s.NextBillingDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalUsageMinutesAsync(Guid organizationId, DateTime? fromDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Subscriptions
            .Where(s => s.OrganizationId == organizationId);

        if (fromDate.HasValue)
        {
            query = query.Where(s => s.CreatedAt >= fromDate.Value);
        }

        return await query.SumAsync(s => s.TranscriptionMinutesUsed, cancellationToken);
    }

    public async Task UpdateUsageAsync(Guid subscriptionId, decimal minutesUsed, CancellationToken cancellationToken = default)
    {
        var subscription = await _context.Subscriptions.FindAsync(new object[] { subscriptionId }, cancellationToken);
        if (subscription != null)
        {
            subscription.TranscriptionMinutesUsed += (int)minutesUsed;
            subscription.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ResetMonthlyUsageAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await _context.Subscriptions.FindAsync(new object[] { subscriptionId }, cancellationToken);
        if (subscription != null)
        {
            subscription.ResetUsage();
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IDictionary<SubscriptionPlan, int>> GetSubscriptionDistributionAsync(CancellationToken cancellationToken = default)
    {
        var distribution = await _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active)
            .GroupBy(s => s.Plan)
            .Select(g => new { Plan = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return distribution.ToDictionary(x => x.Plan, x => x.Count);
    }

    public async Task<decimal> GetMonthlyRecurringRevenueAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active)
            .SumAsync(s => s.MonthlyPrice, cancellationToken);
    }
}
