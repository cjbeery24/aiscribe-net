using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Interfaces;
using SermonTranscription.Domain.Common;
using SermonTranscription.Infrastructure.Data;

namespace SermonTranscription.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Organization entities
/// </summary>
public class OrganizationRepository : BaseRepository<Organization>, IOrganizationRepository
{
    private readonly ILogger<OrganizationRepository> _logger;

    public OrganizationRepository(AppDbContext context, ILogger<OrganizationRepository> logger)
        : base(context)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get organization by slug
    /// </summary>
    public async Task<Organization?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(o => o.Slug == slug, cancellationToken);
    }

    /// <summary>
    /// Check if organization slug exists
    /// </summary>
    public async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(o => o.Slug == slug, cancellationToken);
    }

    /// <summary>
    /// Get active organizations
    /// </summary>
    public async Task<IEnumerable<Organization>> GetActiveOrganizationsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(o => o.IsActive)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get organization with user organizations
    /// </summary>
    public async Task<Organization?> GetWithUserOrganizationsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(o => o.UserOrganizations)
                .ThenInclude(uo => uo.User)
            .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken);
    }

    /// <summary>
    /// Get organization with subscriptions
    /// </summary>
    public async Task<Organization?> GetWithSubscriptionsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(o => o.Subscriptions)
            .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken);
    }

    /// <summary>
    /// Get organization with transcriptions
    /// </summary>
    public async Task<Organization?> GetWithTranscriptionsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(o => o.Transcriptions)
            .Include(o => o.TranscriptionSessions)
            .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken);
    }

    /// <summary>
    /// Search organizations by name
    /// </summary>
    public async Task<IEnumerable<Organization>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(o => o.Name.Contains(searchTerm))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get paginated organizations with filtering and sorting
    /// </summary>
    public async Task<PaginatedResult<Organization>> GetPaginatedOrganizationsAsync(
        PaginationRequest paginationRequest,
        string? searchTerm = null,
        bool? isActive = null,
        bool? hasActiveSubscription = null,
        CancellationToken cancellationToken = default)
    {
        // Build the base query
        var query = _dbSet.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchTermLower = searchTerm.ToLower();
            query = query.Where(o => o.Name.ToLower().Contains(searchTermLower));
        }

        if (isActive.HasValue)
        {
            query = query.Where(o => o.IsActive == isActive.Value);
        }

        if (hasActiveSubscription.HasValue)
        {
            if (hasActiveSubscription.Value)
            {
                query = query.Where(o => o.Subscriptions.Any(s => s.Status == Domain.Enums.SubscriptionStatus.Active));
            }
            else
            {
                query = query.Where(o => !o.Subscriptions.Any(s => s.Status == Domain.Enums.SubscriptionStatus.Active));
            }
        }

        // Get total count before applying pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        var sortBy = paginationRequest.SortBy?.ToLower() ?? "name";
        query = sortBy switch
        {
            "name" => paginationRequest.SortDescending ? query.OrderByDescending(o => o.Name) : query.OrderBy(o => o.Name),
            "createdat" => paginationRequest.SortDescending ? query.OrderByDescending(o => o.CreatedAt) : query.OrderBy(o => o.CreatedAt),
            "updatedat" => paginationRequest.SortDescending ? query.OrderByDescending(o => o.UpdatedAt) : query.OrderBy(o => o.UpdatedAt),
            "isactive" => paginationRequest.SortDescending ? query.OrderByDescending(o => o.IsActive) : query.OrderBy(o => o.IsActive),
            _ => paginationRequest.SortDescending ? query.OrderByDescending(o => o.Name) : query.OrderBy(o => o.Name)
        };

        // Apply pagination
        var items = await query
            .Skip((paginationRequest.PageNumber - 1) * paginationRequest.PageSize)
            .Take(paginationRequest.PageSize)
            .ToListAsync(cancellationToken);

        // Calculate pagination metadata
        var totalPages = (int)Math.Ceiling((double)totalCount / paginationRequest.PageSize);

        return new PaginatedResult<Organization>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = paginationRequest.PageNumber,
            PageSize = paginationRequest.PageSize,
            TotalPages = totalPages,
            HasNextPage = paginationRequest.PageNumber < totalPages,
            HasPreviousPage = paginationRequest.PageNumber > 1
        };
    }
}
