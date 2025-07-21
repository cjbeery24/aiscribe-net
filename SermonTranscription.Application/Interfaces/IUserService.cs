using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Common;

namespace SermonTranscription.Application.Interfaces;

/// <summary>
/// Service interface for user profile management operations
/// </summary>
public interface IUserService
{
    Task<ServiceResult<UserProfileResponse>> GetUserProfileAsync(Guid userId);
    Task<ServiceResult<UserProfileResponse>> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequest request);
    Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task<ServiceResult> DeactivateUserAsync(Guid userId);
    Task<ServiceResult> ActivateUserAsync(Guid userId);
    Task<ServiceResult<OrganizationUserListResponse>> GetOrganizationUsersAsync(Guid organizationId, OrganizationUserSearchRequest request);
    Task<ServiceResult<OrganizationUserResponse>> GetOrganizationUserAsync(Guid organizationId, Guid userId);
    Task<ServiceResult<OrganizationUserResponse>> UpdateOrganizationUserRoleAsync(Guid organizationId, Guid userId, UpdateOrganizationUserRoleRequest request);
    Task<ServiceResult> RemoveUserFromOrganizationAsync(Guid organizationId, Guid userId);
    Task<ServiceResult> DeactivateOrganizationUserAsync(Guid organizationId, Guid userId);
    Task<ServiceResult> ActivateOrganizationUserAsync(Guid organizationId, Guid userId);
}
