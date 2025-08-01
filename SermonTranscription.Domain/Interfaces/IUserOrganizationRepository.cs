using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Common;

namespace SermonTranscription.Domain.Interfaces;

/// <summary>
/// Repository interface for UserOrganization entity operations
/// </summary>
public interface IUserOrganizationRepository : IBaseRepository<UserOrganization>
{
    /// <summary>
    /// Get user's membership in a specific organization
    /// </summary>
    Task<UserOrganization?> GetUserOrganizationAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all organizations where user is active
    /// </summary>
    Task<IEnumerable<UserOrganization>> GetUserOrganizationsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paginated organizations where user is active
    /// </summary>
    Task<PaginatedResult<UserOrganization>> GetPaginatedUserOrganizationsAsync(Guid userId, PaginationRequest request, bool? isActive = null, string? role = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active users in an organization
    /// </summary>
    Task<IEnumerable<UserOrganization>> GetOrganizationUsersAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paginated users in an organization with filtering and sorting
    /// </summary>
    Task<PaginatedResult<UserOrganization>> GetPaginatedOrganizationUsersAsync(
        Guid organizationId,
        PaginationRequest paginationRequest,
        string? searchTerm = null,
        string? role = null,
        bool? isActive = null,
        bool? isEmailVerified = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get users in an organization with specific role
    /// </summary>
    Task<IEnumerable<UserOrganization>> GetOrganizationUsersByRoleAsync(Guid organizationId, UserRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get admin users in an organization
    /// </summary>
    Task<IEnumerable<UserOrganization>> GetOrganizationAdminsAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user is member of organization
    /// </summary>
    Task<bool> IsUserMemberOfOrganizationAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get pending invitations for a user
    /// </summary>
    Task<IEnumerable<UserOrganization>> GetPendingInvitationsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get pending invitations for an organization
    /// </summary>
    Task<IEnumerable<UserOrganization>> GetOrganizationPendingInvitationsAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user-organization relationship by invitation token
    /// </summary>
    Task<UserOrganization?> GetByInvitationTokenAsync(string invitationToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get count of active users in organization
    /// </summary>
    Task<int> GetActiveUserCountAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get count of organizations for user
    /// </summary>
    Task<int> GetUserOrganizationCountAsync(Guid userId, CancellationToken cancellationToken = default);
}
