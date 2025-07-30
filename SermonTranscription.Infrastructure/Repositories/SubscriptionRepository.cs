using Microsoft.EntityFrameworkCore;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Interfaces;
using SermonTranscription.Domain.Common;
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

    public async Task<PaginatedResult<Subscription>> GetPaginatedByOrganizationAsync(Guid organizationId, PaginationRequest request, string? status = null, string? plan = null, CancellationToken cancellationToken = default)
    {
        // Build the base query
        var query = _context.Subscriptions
            .Include(s => s.Organization)
            .Where(s => s.OrganizationId == organizationId);

        // Apply filters
        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<SubscriptionStatus>(status, true, out var statusEnum))
            {
                query = query.Where(s => s.Status == statusEnum);
            }
        }

        if (!string.IsNullOrEmpty(plan))
        {
            if (Enum.TryParse<SubscriptionPlan>(plan, true, out var planEnum))
            {
                query = query.Where(s => s.Plan == planEnum);
            }
        }

        // Get total count before applying pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = request.SortBy?.ToLowerInvariant() switch
        {
            "createdat" => request.SortDescending
                ? query.OrderByDescending(s => s.CreatedAt)
                : query.OrderBy(s => s.CreatedAt),
            "updatedat" => request.SortDescending
                ? query.OrderByDescending(s => s.UpdatedAt)
                : query.OrderBy(s => s.UpdatedAt),
            "plan" => request.SortDescending
                ? query.OrderByDescending(s => s.Plan)
                : query.OrderBy(s => s.Plan),
            "status" => request.SortDescending
                ? query.OrderByDescending(s => s.Status)
                : query.OrderBy(s => s.Status),
            "price" => request.SortDescending
                ? query.OrderByDescending(s => s.MonthlyPrice)
                : query.OrderBy(s => s.MonthlyPrice),
            _ => request.SortDescending
                ? query.OrderByDescending(s => s.CreatedAt)
                : query.OrderBy(s => s.CreatedAt)
        };

        // Apply pagination
        var skip = (request.PageNumber - 1) * request.PageSize;
        var items = await query
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Calculate pagination metadata
        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        return new PaginatedResult<Subscription>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = totalPages,
            HasNextPage = request.PageNumber < totalPages,
            HasPreviousPage = request.PageNumber > 1
        };
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

    public async Task<int> GetTotalUsageMinutesAsync(Guid organizationId, DateTime? fromDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Subscriptions
            .Where(s => s.OrganizationId == organizationId);

        if (fromDate.HasValue)
        {
            query = query.Where(s => s.CreatedAt >= fromDate.Value);
        }

        return await query.SumAsync(s => s.TranscriptionMinutesUsed, cancellationToken);
    }

    public async Task UpdateUsageAsync(Guid subscriptionId, int minutesUsed, CancellationToken cancellationToken = default)
    {
        var subscription = await _context.Subscriptions.FindAsync(new object[] { subscriptionId }, cancellationToken);
        if (subscription != null)
        {
            subscription.TranscriptionMinutesUsed += minutesUsed;
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
