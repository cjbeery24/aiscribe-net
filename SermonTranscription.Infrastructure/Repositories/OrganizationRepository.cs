using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Interfaces;
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
}
