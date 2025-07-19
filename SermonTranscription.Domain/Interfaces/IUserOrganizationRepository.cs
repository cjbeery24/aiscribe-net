using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;

namespace SermonTranscription.Domain.Interfaces;

/// <summary>
/// Repository interface for UserOrganization entity operations
/// </summary>
public interface IUserOrganizationRepository : IBaseRepository<UserOrganization>
{
    /// <summary>
    /// Get user's membership in a specific organization
    /// </summary>
    Task<UserOrganization?> GetUserOrganizationAsync(Guid userId, Guid organizationId);
    
    /// <summary>
    /// Get all organizations where user is active
    /// </summary>
    Task<IEnumerable<UserOrganization>> GetUserOrganizationsAsync(Guid userId);
    
    /// <summary>
    /// Get all active users in an organization
    /// </summary>
    Task<IEnumerable<UserOrganization>> GetOrganizationUsersAsync(Guid organizationId);
    
    /// <summary>
    /// Get users in an organization with specific role
    /// </summary>
    Task<IEnumerable<UserOrganization>> GetOrganizationUsersByRoleAsync(Guid organizationId, UserRole role);
    
    /// <summary>
    /// Get admin users in an organization
    /// </summary>
    Task<IEnumerable<UserOrganization>> GetOrganizationAdminsAsync(Guid organizationId);
    
    /// <summary>
    /// Check if user is member of organization
    /// </summary>
    Task<bool> IsUserMemberOfOrganizationAsync(Guid userId, Guid organizationId);
    
    /// <summary>
    /// Get pending invitations for a user
    /// </summary>
    Task<IEnumerable<UserOrganization>> GetPendingInvitationsAsync(Guid userId);
    
    /// <summary>
    /// Get pending invitations for an organization
    /// </summary>
    Task<IEnumerable<UserOrganization>> GetOrganizationPendingInvitationsAsync(Guid organizationId);
    
    /// <summary>
    /// Get user-organization relationship by invitation token
    /// </summary>
    Task<UserOrganization?> GetByInvitationTokenAsync(string invitationToken);
    
    /// <summary>
    /// Get count of active users in organization
    /// </summary>
    Task<int> GetActiveUserCountAsync(Guid organizationId);
    
    /// <summary>
    /// Get count of organizations for user
    /// </summary>
    Task<int> GetUserOrganizationCountAsync(Guid userId);
} 