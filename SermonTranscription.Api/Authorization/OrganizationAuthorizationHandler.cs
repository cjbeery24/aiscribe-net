using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Interfaces;
using System.Security.Claims;

namespace SermonTranscription.Api.Authorization;

/// <summary>
/// Authorization handler for organization-scoped permissions
/// </summary>
public class OrganizationAuthorizationHandler : AuthorizationHandler<OrganizationRequirement>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<OrganizationAuthorizationHandler> _logger;

    public OrganizationAuthorizationHandler(
        IUserRepository userRepository,
        ILogger<OrganizationAuthorizationHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrganizationRequirement requirement)
    {
        try
        {
            // Get user ID from claims
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("User ID claim not found or invalid in authorization context");
                return;
            }

            // Get organization ID from claims
            var organizationIdClaim = context.User.FindFirst("organization_id");
            if (organizationIdClaim == null || !Guid.TryParse(organizationIdClaim.Value, out var organizationId))
            {
                _logger.LogWarning("Organization ID claim not found or invalid in authorization context");
                return;
            }

            // Get user with organization membership
            var user = await _userRepository.GetByIdWithOrganizationsAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found during authorization: {UserId}", userId);
                return;
            }

            // Check if user is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Inactive user attempted authorization: {UserId}", userId);
                return;
            }

            // Get user's membership in the organization
            var membership = user.GetOrganizationMembership(organizationId);
            if (membership == null)
            {
                _logger.LogWarning("User {UserId} is not a member of organization {OrganizationId}", userId, organizationId);
                return;
            }

            // Check if user is active in the organization
            if (!membership.IsActive)
            {
                _logger.LogWarning("User {UserId} is not active in organization {OrganizationId}", userId, organizationId);
                return;
            }

            // Validate the specific requirement
            var isAuthorized = requirement.PermissionType switch
            {
                OrganizationPermissionType.Admin => membership.IsAdmin(),
                OrganizationPermissionType.ManageUsers => membership.CanManageUsers(),
                OrganizationPermissionType.ManageTranscriptions => membership.CanManageTranscriptions(),
                OrganizationPermissionType.ViewTranscriptions => membership.CanViewTranscriptions(),
                OrganizationPermissionType.ExportTranscriptions => membership.CanExportTranscriptions(),
                OrganizationPermissionType.Member => true, // Already validated membership above
                _ => false
            };

            if (isAuthorized)
            {
                context.Succeed(requirement);
                _logger.LogDebug("Authorization succeeded for user {UserId} in organization {OrganizationId} for permission {PermissionType}", 
                    userId, organizationId, requirement.PermissionType);
            }
            else
            {
                _logger.LogWarning("Authorization failed for user {UserId} in organization {OrganizationId} for permission {PermissionType}", 
                    userId, organizationId, requirement.PermissionType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during organization authorization");
        }
    }
}

/// <summary>
/// Requirement for organization-scoped permissions
/// </summary>
public class OrganizationRequirement : IAuthorizationRequirement
{
    public OrganizationPermissionType PermissionType { get; }

    public OrganizationRequirement(OrganizationPermissionType permissionType)
    {
        PermissionType = permissionType;
    }
}

/// <summary>
/// Types of permissions that can be required for organization access
/// </summary>
public enum OrganizationPermissionType
{
    Admin,
    ManageUsers,
    ManageTranscriptions,
    ViewTranscriptions,
    ExportTranscriptions,
    Member
} 