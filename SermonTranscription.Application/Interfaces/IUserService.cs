using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Common;

namespace SermonTranscription.Application.Interfaces;

/// <summary>
/// Service interface for user profile management operations
/// </summary>
public interface IUserService
{
    Task<ServiceResult<UserProfileResponse>> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ServiceResult<UserProfileResponse>> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ServiceResult> ActivateUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ServiceResult<OrganizationUserListResponse>> GetOrganizationUsersAsync(Guid organizationId, OrganizationUserSearchRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<OrganizationUserResponse>> GetOrganizationUserAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    Task<ServiceResult<OrganizationUserResponse>> UpdateOrganizationUserRoleAsync(Guid organizationId, Guid userId, UpdateOrganizationUserRoleRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> RemoveUserFromOrganizationAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeactivateOrganizationUserAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    Task<ServiceResult> ActivateOrganizationUserAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
}
