using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Common;

namespace SermonTranscription.Application.Interfaces;

/// <summary>
/// Service interface for organization management operations
/// </summary>
public interface IOrganizationService
{
    Task<ServiceResult<OrganizationResponse>> CreateOrganizationAsync(CreateOrganizationRequest request, Guid createdByUserId);
    Task<ServiceResult<OrganizationResponse>> GetOrganizationAsync(Guid organizationId);
    Task<ServiceResult<OrganizationResponse>> GetOrganizationBySlugAsync(string slug);
    Task<ServiceResult<OrganizationListResponse>> GetOrganizationsAsync(OrganizationSearchRequest request);
    Task<ServiceResult<OrganizationResponse>> UpdateOrganizationAsync(Guid organizationId, UpdateOrganizationRequest request);
    Task<ServiceResult<OrganizationResponse>> UpdateOrganizationSettingsAsync(Guid organizationId, UpdateOrganizationSettingsRequest request);
    Task<ServiceResult<OrganizationResponse>> UpdateOrganizationLogoAsync(Guid organizationId, UpdateOrganizationLogoRequest request);
    Task<ServiceResult> DeleteOrganizationAsync(Guid organizationId);
    Task<ServiceResult> ActivateOrganizationAsync(Guid organizationId);
    Task<ServiceResult> DeactivateOrganizationAsync(Guid organizationId);
    Task<ServiceResult<OrganizationWithUsersResponse>> GetOrganizationWithUsersAsync(Guid organizationId);
    Task<ServiceResult<OrganizationWithSubscriptionsResponse>> GetOrganizationWithSubscriptionsAsync(Guid organizationId);
}
