using System.Linq.Expressions;

namespace SermonTranscription.Domain.Interfaces;

/// <summary>
/// Multi-tenant repository interface for entities that belong to organizations
/// </summary>
/// <typeparam name="T">The entity type that implements IMultiTenantEntity</typeparam>
public interface IMultiTenantRepository<T> : IBaseRepository<T> where T : class, IMultiTenantEntity
{
    // Organization-scoped read operations
    Task<T?> GetByIdAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, Guid organizationId, CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, Guid organizationId, CancellationToken cancellationToken = default);

    // Organization-scoped query operations
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, Guid organizationId, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate, Guid organizationId, CancellationToken cancellationToken = default);

    // Pagination support for organization-scoped queries
    Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        Guid organizationId,
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, object>>? orderBy = null,
        bool ascending = true,
        CancellationToken cancellationToken = default);

    // Bulk operations within organization scope
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, Guid organizationId, CancellationToken cancellationToken = default);
    Task DeleteRangeAsync(IEnumerable<T> entities, Guid organizationId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid organizationId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for entities that belong to an organization (multi-tenant)
/// </summary>
public interface IMultiTenantEntity
{
    Guid OrganizationId { get; set; }
}
