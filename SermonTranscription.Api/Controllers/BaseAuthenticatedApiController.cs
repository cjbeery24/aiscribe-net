using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SermonTranscription.Application.Common;
using SermonTranscription.Application.DTOs;
using System.Security.Claims;

namespace SermonTranscription.Api.Controllers;

/// <summary>
/// Base API controller with authorization and user context for authenticated endpoints
/// </summary>
[ApiController]
[Authorize]
public abstract class BaseAuthenticatedApiController : BaseApiController
{
    protected BaseAuthenticatedApiController(ILogger logger) : base(logger)
    {
    }

    #region Protected Helper Methods

    /// <summary>
    /// Gets the current user ID from the JWT token claims
    /// </summary>
    /// <returns>User ID if found, null otherwise</returns>
    protected Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return !string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Gets the current organization ID from the JWT token claims
    /// </summary>
    /// <returns>Organization ID if found, null otherwise</returns>
    protected Guid? GetCurrentOrganizationId()
    {
        var organizationIdClaim = User.FindFirst("OrganizationId")?.Value;
        return !string.IsNullOrEmpty(organizationIdClaim) && Guid.TryParse(organizationIdClaim, out var organizationId) ? organizationId : null;
    }

    /// <summary>
    /// Creates a standardized error response for unauthorized access using ApiResponse wrapper
    /// </summary>
    /// <param name="message">Error message</param>
    /// <returns>Unauthorized response</returns>
    protected IActionResult UnauthorizedError(string message = "User not authenticated")
    {
        return UnauthorizedError(message, ["Valid authentication required"]);
    }

    /// <summary>
    /// Creates a standardized error response for forbidden access using ApiResponse wrapper
    /// </summary>
    /// <param name="message">Error message</param>
    /// <returns>Forbidden response</returns>
    protected IActionResult ForbiddenError(string message = "Access denied")
    {
        return ForbiddenError(message, ["Insufficient permissions"]);
    }

    /// <summary>
    /// Creates a standardized error response for not found using ApiResponse wrapper
    /// </summary>
    /// <param name="message">Error message</param>
    /// <returns>Not found response</returns>
    protected IActionResult NotFoundError(string message = "Resource not found")
    {
        return NotFoundError(message, [message]);
    }

    #endregion
}
