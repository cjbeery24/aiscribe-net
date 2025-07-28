using Microsoft.AspNetCore.Mvc;
using SermonTranscription.Api.Authorization;
using SermonTranscription.Api.Middleware;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Interfaces;

namespace SermonTranscription.Api.Controllers;

/// <summary>
/// User management controller for profile operations and organization user management
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
    [OrganizationAgnostic]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserProfile()
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _userService.GetUserProfileAsync(userId, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => Ok(result.Data));
    }

    /// <summary>
    /// Update current user's profile
    /// </summary>
    /// <param name="request">Profile update request</param>
    /// <returns>Updated user profile</returns>
    [HttpPut("profile")]
    [OrganizationAgnostic]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateUserProfileRequest request)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _userService.UpdateUserProfileAsync(userId, request, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => Ok(result.Data));
    }

    /// <summary>
    /// Change current user's password
    /// </summary>
    /// <param name="request">Password change request</param>
    /// <returns>Password change confirmation</returns>
    [HttpPost("change-password")]
    [OrganizationAgnostic]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _userService.ChangePasswordAsync(userId, request, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => SuccessResponse("Password changed successfully"));
    }

    /// <summary>
    /// Get organization users with search and pagination
    /// </summary>
    /// <param name="request">Search and pagination parameters</param>
    /// <returns>List of organization users</returns>
    [HttpGet]
    [RequireCanManageUsers]
    [ProducesResponseType(typeof(OrganizationUserListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrganizationUsers([FromQuery] OrganizationUserSearchRequest request)
    {
        var tenantContext = HttpContext.GetTenantContext()!;
        var result = await _userService.GetOrganizationUsersAsync(tenantContext.OrganizationId, request, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => Ok(result.Data));
    }

    /// <summary>
    /// Get specific organization user by ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Organization user details</returns>
    [HttpGet("{userId:guid}")]
    [RequireCanManageUsers]
    [ProducesResponseType(typeof(OrganizationUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrganizationUser(Guid userId)
    {
        var tenantContext = HttpContext.GetTenantContext()!;
        var result = await _userService.GetOrganizationUserAsync(tenantContext.OrganizationId, userId, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => Ok(result.Data));
    }

    /// <summary>
    /// Update organization user role
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Role update request</param>
    /// <returns>Updated organization user details</returns>
    [HttpPut("{userId:guid}/role")]
    [RequireCanManageUsers]
    [ProducesResponseType(typeof(OrganizationUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrganizationUserRole(Guid userId, [FromBody] UpdateOrganizationUserRoleRequest request)
    {
        var tenantContext = HttpContext.GetTenantContext()!;
        var result = await _userService.UpdateOrganizationUserRoleAsync(tenantContext.OrganizationId, userId, request, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => Ok(result.Data));
    }

    /// <summary>
    /// Remove user from organization
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Removal confirmation</returns>
    [HttpDelete("{userId:guid}")]
    [RequireCanManageUsers]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveUserFromOrganization(Guid userId)
    {
        var tenantContext = HttpContext.GetTenantContext()!;
        var result = await _userService.RemoveUserFromOrganizationAsync(tenantContext.OrganizationId, userId, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => SuccessResponse("User removed from organization successfully"));
    }

    /// <summary>
    /// Deactivate organization user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Deactivation confirmation</returns>
    [HttpPost("{userId:guid}/deactivate")]
    [RequireCanManageUsers]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateOrganizationUser(Guid userId)
    {
        var tenantContext = HttpContext.GetTenantContext()!;
        var result = await _userService.DeactivateOrganizationUserAsync(tenantContext.OrganizationId, userId, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => SuccessResponse("User deactivated successfully"));
    }

    /// <summary>
    /// Activate organization user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Activation confirmation</returns>
    [HttpPost("{userId:guid}/activate")]
    [RequireCanManageUsers]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateOrganizationUser(Guid userId)
    {
        var tenantContext = HttpContext.GetTenantContext()!;
        var result = await _userService.ActivateOrganizationUserAsync(tenantContext.OrganizationId, userId, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => SuccessResponse("User activated successfully"));
    }
}
