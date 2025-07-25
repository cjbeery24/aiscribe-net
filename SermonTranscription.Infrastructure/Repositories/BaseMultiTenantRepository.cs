using Microsoft.EntityFrameworkCore;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Interfaces;
using SermonTranscription.Infrastructure.Data;
using System.Linq.Expressions;

namespace SermonTranscription.Infrastructure.Repositories;

/// <summary>
/// Base multi-tenant repository implementation providing organization-scoped CRUD operations
/// </summary>
/// <typeparam name="T">The entity type that implements IMultiTenantEntity</typeparam>
public abstract class BaseMultiTenantRepository<T> : BaseRepository<T>, IMultiTenantRepository<T> where T : class, IMultiTenantEntity
{
    protected BaseMultiTenantRepository(AppDbContext context) : base(context)
    {
    }

    // Override base methods to ensure organization scoping
    public override Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // This method should not be used directly for multi-tenant entities
        // Use GetByIdAsync(id, organizationId, cancellationToken) instead
        throw new InvalidOperationException($"Use GetByIdAsync(id, organizationId, cancellationToken) for multi-tenant entity {typeof(T).Name}");
    }

    public override Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // This method should not be used directly for multi-tenant entities
        // Use GetAllAsync(organizationId, cancellationToken) instead
        throw new InvalidOperationException($"Use GetAllAsync(organizationId, cancellationToken) for multi-tenant entity {typeof(T).Name}");
    }

    // Organization-scoped read operations
    public virtual async Task<T?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(e =>
            EF.Property<Guid>(e, "Id") == id &&
            e.OrganizationId == organizationId,
            cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.OrganizationId == organizationId)
                          .ToListAsync(cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.OrganizationId == organizationId)
                          .Where(predicate)
                          .ToListAsync(cancellationToken);
    }

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.OrganizationId == organizationId)
                          .FirstOrDefaultAsync(predicate, cancellationToken);
    }

    // Organization-scoped query operations
    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.OrganizationId == organizationId)
                          .AnyAsync(predicate, cancellationToken);
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(e => e.OrganizationId == organizationId);

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.CountAsync(cancellationToken);
    }

    // Pagination support for organization-scoped queries
    public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        Guid organizationId,
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, object>>? orderBy = null,
        bool ascending = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(e => e.OrganizationId == organizationId);

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (orderBy != null)
        {
            query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);
        }

        var items = await query.Skip((pageNumber - 1) * pageSize)
                              .Take(pageSize)
                              .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    // Override write operations to ensure organization scoping
    public override async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        // Ensure the entity has an organization ID set
        if (entity.OrganizationId == Guid.Empty)
        {
            throw new InvalidOperationException($"OrganizationId must be set for multi-tenant entity {typeof(T).Name}");
        }

        return await base.AddAsync(entity, cancellationToken);
    }

    public override async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        // Ensure the entity has an organization ID set
        if (entity.OrganizationId == Guid.Empty)
        {
            throw new InvalidOperationException($"OrganizationId must be set for multi-tenant entity {typeof(T).Name}");
        }

        await base.UpdateAsync(entity, cancellationToken);
    }

    // Organization-scoped bulk operations
    public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, Guid organizationId, CancellationToken cancellationToken = default)
    {
        // Ensure all entities have the correct organization ID
        foreach (var entity in entities)
        {
            entity.OrganizationId = organizationId;
        }

        return await base.AddRangeAsync(entities, cancellationToken);
    }

    public virtual async Task DeleteRangeAsync(IEnumerable<T> entities, Guid organizationId, CancellationToken cancellationToken = default)
    {
        // Ensure all entities belong to the specified organization
        var organizationEntities = entities.Where(e => e.OrganizationId == organizationId);
        await base.DeleteRangeAsync(organizationEntities, cancellationToken);
    }

    public virtual async Task DeleteAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, organizationId, cancellationToken);
        if (entity != null)
        {
            await base.DeleteAsync(entity, cancellationToken);
        }
    }

    // Helper method to create organization-scoped query
    protected IQueryable<T> CreateOrganizationQuery(Guid organizationId)
    {
        return _dbSet.Where(e => e.OrganizationId == organizationId);
    }

    // Helper method to validate organization access
    protected void ValidateOrganizationAccess(T entity, Guid organizationId)
    {
        if (entity.OrganizationId != organizationId)
        {
            throw new UnauthorizedAccessException($"Entity {typeof(T).Name} with ID {entity.GetType().GetProperty("Id")?.GetValue(entity)} does not belong to organization {organizationId}");
        }
    }
}
