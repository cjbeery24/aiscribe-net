using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Common;

namespace SermonTranscription.Application.Interfaces;

/// <summary>
/// Service interface for organization management operations
/// </summary>
public interface IOrganizationService
{
    Task<ServiceResult<OrganizationResponse>> CreateOrganizationAsync(CreateOrganizationRequest request, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task<ServiceResult<OrganizationResponse>> GetOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<ServiceResult<OrganizationResponse>> GetOrganizationBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<ServiceResult<OrganizationListResponse>> GetOrganizationsAsync(OrganizationSearchRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<OrganizationResponse>> UpdateOrganizationAsync(Guid organizationId, UpdateOrganizationRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<OrganizationResponse>> UpdateOrganizationSettingsAsync(Guid organizationId, UpdateOrganizationSettingsRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<OrganizationResponse>> UpdateOrganizationLogoAsync(Guid organizationId, UpdateOrganizationLogoRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> ActivateOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeactivateOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<ServiceResult<OrganizationWithUsersResponse>> GetOrganizationWithUsersAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<ServiceResult<OrganizationWithSubscriptionsResponse>> GetOrganizationWithSubscriptionsAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<ServiceResult<OrganizationDashboardResponse>> GetOrganizationDashboardAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
