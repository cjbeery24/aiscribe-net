using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SermonTranscription.Api.Authorization;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Interfaces;

namespace SermonTranscription.Api.Controllers;

/// <summary>
/// User profile management and organization user controller
/// </summary>
[Route("api/v{version:apiVersion}/users")]
[ApiVersion("1.0")]
public class UsersController : BaseApiController
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
    [RequireAuthenticatedUser]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return UnauthorizedError();
            }

            var result = await _userService.GetUserProfileAsync(userId.Value);
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
    [RequireAuthenticatedUser]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateUserProfileRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return UnauthorizedError();
            }

            var result = await _userService.UpdateUserProfileAsync(userId.Value, request);
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
    [RequireAuthenticatedUser]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return UnauthorizedError();
            }

            var result = await _userService.ChangePasswordAsync(userId.Value, request);
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
    [HttpGet("organizations/{organizationId:guid}/users")]
    [RequireCanManageUsers]
    [ProducesResponseType(typeof(OrganizationUserListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOrganizationUsers(Guid organizationId, [FromQuery] OrganizationUserSearchRequest request)
    {
        try
        {
            var result = await _userService.GetOrganizationUsersAsync(organizationId, request);
            return HandleServiceResult(result, () => Ok(result.Data));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error retrieving organization users for organization {organizationId}");
        }
    }

    /// <summary>
    /// Get specific organization user
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Organization user details</returns>
    [HttpGet("organizations/{organizationId:guid}/users/{userId:guid}")]
    [RequireCanManageUsers]
    [ProducesResponseType(typeof(OrganizationUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrganizationUser(Guid organizationId, Guid userId)
    {
        try
        {
            var result = await _userService.GetOrganizationUserAsync(organizationId, userId);
            return HandleServiceResult(result, () => Ok(result.Data));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error retrieving organization user {userId} for organization {organizationId}");
        }
    }

    /// <summary>
    /// Update organization user role
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="request">Role update request</param>
    /// <returns>Updated organization user</returns>
    [HttpPut("organizations/{organizationId:guid}/users/{userId:guid}/role")]
    [RequireCanManageUsers]
    [ProducesResponseType(typeof(OrganizationUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrganizationUserRole(Guid organizationId, Guid userId, [FromBody] UpdateOrganizationUserRoleRequest request)
    {
        try
        {
            var result = await _userService.UpdateOrganizationUserRoleAsync(organizationId, userId, request);
            return HandleServiceResult(result, () => Ok(result.Data));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error updating organization user role {userId} for organization {organizationId}");
        }
    }

    /// <summary>
    /// Remove user from organization
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Removal confirmation</returns>
    [HttpDelete("organizations/{organizationId:guid}/users/{userId:guid}")]
    [RequireCanManageUsers]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveUserFromOrganization(Guid organizationId, Guid userId)
    {
        try
        {
            var result = await _userService.RemoveUserFromOrganizationAsync(organizationId, userId);
            return HandleServiceResult(result, () => Ok(new SuccessResponse { Message = result.Message }));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error removing user {userId} from organization {organizationId}");
        }
    }

    /// <summary>
    /// Deactivate organization user
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Deactivation confirmation</returns>
    [HttpPost("organizations/{organizationId:guid}/users/{userId:guid}/deactivate")]
    [RequireCanManageUsers]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeactivateOrganizationUser(Guid organizationId, Guid userId)
    {
        try
        {
            var result = await _userService.DeactivateOrganizationUserAsync(organizationId, userId);
            return HandleServiceResult(result, () => Ok(new SuccessResponse { Message = result.Message }));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error deactivating organization user {userId} in organization {organizationId}");
        }
    }

    /// <summary>
    /// Activate organization user
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Activation confirmation</returns>
    [HttpPost("organizations/{organizationId:guid}/users/{userId:guid}/activate")]
    [RequireCanManageUsers]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ActivateOrganizationUser(Guid organizationId, Guid userId)
    {
        try
        {
            var result = await _userService.ActivateOrganizationUserAsync(organizationId, userId);
            return HandleServiceResult(result, () => Ok(new SuccessResponse { Message = result.Message }));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error activating organization user {userId} in organization {organizationId}");
        }
    }

}
