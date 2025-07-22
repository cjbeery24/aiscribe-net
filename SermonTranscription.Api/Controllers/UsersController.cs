using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SermonTranscription.Api.Authorization;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Interfaces;
using SermonTranscription.Api.Middleware;

namespace SermonTranscription.Api.Controllers;

/// <summary>
/// User profile management and organization user controller
/// </summary>
[Route("api/v{version:apiVersion}/users")]
[ApiVersion("1.0")]
public class UsersController : BaseAuthenticatedApiController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
        : base(logger)
    {
        _userService = userService;
    }

    /// <summary>
    /// Get current user's profile
    /// </summary>
    /// <returns>User profile information</returns>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserProfile()
    {
        try
        {
            var userId = HttpContext.GetUserId()!.Value;

            var result = await _userService.GetUserProfileAsync(userId);
            return HandleServiceResult(result, () => Ok(result.Data));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Error retrieving user profile");
        }
    }

    /// <summary>
    /// Update current user's profile
    /// </summary>
    /// <param name="request">Profile update request</param>
    /// <returns>Updated user profile</returns>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateUserProfileRequest request)
    {
        try
        {
            var userId = HttpContext.GetUserId()!.Value;

            var result = await _userService.UpdateUserProfileAsync(userId, request);
            return HandleServiceResult(result, () => Ok(result.Data));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Error updating user profile");
        }
    }

    /// <summary>
    /// Change current user's password
    /// </summary>
    /// <param name="request">Password change request</param>
    /// <returns>Password change confirmation</returns>
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userId = HttpContext.GetUserId()!.Value;

            var result = await _userService.ChangePasswordAsync(userId, request);
            return HandleServiceResult(result, () => Ok(new SuccessResponse { Message = result.Message }));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Error changing password");
        }
    }

    /// <summary>
    /// Get organization users with search and pagination
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="request">Search and pagination parameters</param>
    /// <returns>List of organization users</returns>
    [HttpGet("organizations/users")]
    [RequireCanManageUsers]
    [ProducesResponseType(typeof(OrganizationUserListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOrganizationUsers([FromQuery] OrganizationUserSearchRequest request)
    {
        try
        {
            var tenantContext = HttpContext.GetTenantContext()!;
            var result = await _userService.GetOrganizationUsersAsync(tenantContext.OrganizationId, request);
            return HandleServiceResult(result, () => Ok(result.Data));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error retrieving organization users");
        }
    }

    /// <summary>
    /// Get specific organization user
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Organization user details</returns>
    [HttpGet("organizations/users/{userId:guid}")]
    [RequireCanManageUsers]
    [ProducesResponseType(typeof(OrganizationUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrganizationUser(Guid userId)
    {
        try
        {
            var tenantContext = HttpContext.GetTenantContext()!;
            var result = await _userService.GetOrganizationUserAsync(tenantContext.OrganizationId, userId);
            return HandleServiceResult(result, () => Ok(result.Data));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error retrieving organization user {userId}");
        }
    }

    /// <summary>
    /// Update organization user role
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="request">Role update request</param>
    /// <returns>Updated organization user</returns>
    [HttpPut("organizations/users/{userId:guid}/role")]
    [RequireCanManageUsers]
    [ProducesResponseType(typeof(OrganizationUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrganizationUserRole(Guid userId, [FromBody] UpdateOrganizationUserRoleRequest request)
    {
        try
        {
            var tenantContext = HttpContext.GetTenantContext()!;
            var result = await _userService.UpdateOrganizationUserRoleAsync(tenantContext.OrganizationId, userId, request);
            return HandleServiceResult(result, () => Ok(result.Data));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error updating organization user role {userId}");
        }
    }

    /// <summary>
    /// Remove user from organization
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Removal confirmation</returns>
    [HttpDelete("organizations/users/{userId:guid}")]
    [RequireCanManageUsers]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveUserFromOrganization(Guid userId)
    {
        try
        {
            var tenantContext = HttpContext.GetTenantContext()!;
            var result = await _userService.RemoveUserFromOrganizationAsync(tenantContext.OrganizationId, userId);
            return HandleServiceResult(result, () => Ok(new SuccessResponse { Message = result.Message }));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error removing user {userId}");
        }
    }

    /// <summary>
    /// Deactivate organization user
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Deactivation confirmation</returns>
    [HttpPost("organizations/users/{userId:guid}/deactivate")]
    [RequireCanManageUsers]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeactivateOrganizationUser(Guid userId)
    {
        try
        {
            var tenantContext = HttpContext.GetTenantContext()!;
            var result = await _userService.DeactivateOrganizationUserAsync(tenantContext.OrganizationId, userId);
            return HandleServiceResult(result, () => Ok(new SuccessResponse { Message = result.Message }));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error deactivating organization user {userId}");
        }
    }

    /// <summary>
    /// Activate organization user
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Activation confirmation</returns>
    [HttpPost("organizations/users/{userId:guid}/activate")]
    [RequireCanManageUsers]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ActivateOrganizationUser(Guid userId)
    {
        try
        {
            var tenantContext = HttpContext.GetTenantContext()!;
            var result = await _userService.ActivateOrganizationUserAsync(tenantContext.OrganizationId, userId);
            return HandleServiceResult(result, () => Ok(new SuccessResponse { Message = result.Message }));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error activating organization user {userId}");
        }
    }

}
