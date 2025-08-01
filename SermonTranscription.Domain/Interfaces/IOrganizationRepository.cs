using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Common;

namespace SermonTranscription.Domain.Interfaces;

/// <summary>
/// Repository interface for Organization entity with organization-specific operations
/// </summary>
public interface IOrganizationRepository : IBaseRepository<Organization>
{
    // Organization-specific query methods
    Task<Organization?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);
    Task<IEnumerable<Organization>> GetActiveOrganizationsAsync(CancellationToken cancellationToken = default);

    // Organization management methods
    Task<Organization?> GetWithUserOrganizationsAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<Organization?> GetWithSubscriptionsAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<Organization?> GetWithTranscriptionsAsync(Guid organizationId, CancellationToken cancellationToken = default);

    // Search and filtering
    Task<IEnumerable<Organization>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);

    // Pagination and search
    Task<PaginatedResult<Organization>> GetPaginatedOrganizationsAsync(
        PaginationRequest paginationRequest,
        string? searchTerm = null,
        bool? isActive = null,
        bool? hasActiveSubscription = null,
        CancellationToken cancellationToken = default);
}
