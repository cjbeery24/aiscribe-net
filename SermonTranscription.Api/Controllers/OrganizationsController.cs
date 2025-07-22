using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SermonTranscription.Api.Authorization;
using SermonTranscription.Api.Middleware;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Services;
using SermonTranscription.Application.Interfaces;

namespace SermonTranscription.Api.Controllers;

/// <summary>
/// Organization management controller with CRUD operations
/// </summary>
[Route("api/v{version:apiVersion}/organizations")]
[ApiVersion("1.0")]
public class OrganizationsController : BaseAuthenticatedApiController
{
    private readonly IOrganizationService _organizationService;

    public OrganizationsController(IOrganizationService organizationService, ILogger<OrganizationsController> logger)
        : base(logger)
    {
        _organizationService = organizationService;
    }

    /// <summary>
    /// Create a new organization
    /// </summary>
    /// <param name="request">Organization creation request</param>
    /// <returns>Created organization details</returns>
    [HttpPost]
    [OrganizationAgnostic]
    [ProducesResponseType(typeof(OrganizationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationRequest request)
    {
        try
        {
            var validationResult = ValidateModelState();
            if (validationResult != null) return validationResult;

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ErrorResponse
                {
                    Message = "User not authenticated",
                    Errors = ["Valid authentication required"]
                });
            }

            var result = await _organizationService.CreateOrganizationAsync(request, userId.Value);
            return HandleServiceResult<OrganizationResponse>(result, () => Ok(result.Data));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Error creating organization");
        }
    }

    /// <summary>
    /// Get organization by ID
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <returns>Organization details</returns>
    [HttpGet("load")]
    [ProducesResponseType(typeof(OrganizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LoadOrganization()
    {
        try
        {
            var tenantContext = HttpContext.GetTenantContext()!;
            var result = await _organizationService.GetOrganizationAsync(tenantContext.OrganizationId);
            return HandleServiceResult<OrganizationResponse>(result, () => Ok(result.Data));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error retrieving organization");
        }
    }


    /// <summary>
    /// Update organization details
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated organization details</returns>
    [HttpPut]
    [RequireOrganizationAdmin]
    [ProducesResponseType(typeof(OrganizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrganization([FromBody] UpdateOrganizationRequest request)
    {
        try
        {
            var validationResult = ValidateModelState();
            if (validationResult != null) return validationResult;

            var tenantContext = HttpContext.GetTenantContext()!;
            var result = await _organizationService.UpdateOrganizationAsync(tenantContext.OrganizationId, request);
            return HandleServiceResult(result, () => Ok(result.Data));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error updating organization");
        }
    }

    /// <summary>
    /// Update organization settings
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="request">Settings update request</param>
    /// <returns>Updated organization details</returns>
    [HttpPut("settings")]
    [RequireOrganizationAdmin]
    [ProducesResponseType(typeof(OrganizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrganizationSettings([FromBody] UpdateOrganizationSettingsRequest request)
    {
        try
        {
            var validationResult = ValidateModelState();
            if (validationResult != null) return validationResult;

            var tenantContext = HttpContext.GetTenantContext()!;
            var result = await _organizationService.UpdateOrganizationSettingsAsync(tenantContext.OrganizationId, request);
            return HandleServiceResult(result, () => Ok(result.Data));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error updating organization settings");
        }
    }

    /// <summary>
    /// Update organization logo
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="request">Logo update request</param>
    /// <returns>Updated organization details</returns>
    [HttpPut("logo")]
    [RequireOrganizationAdmin]
    [ProducesResponseType(typeof(OrganizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrganizationLogo([FromBody] UpdateOrganizationLogoRequest request)
    {
        try
        {
            var validationResult = ValidateModelState();
            if (validationResult != null) return validationResult;

            var tenantContext = HttpContext.GetTenantContext()!;
            var result = await _organizationService.UpdateOrganizationLogoAsync(tenantContext.OrganizationId, request);
            return HandleServiceResult(result, () => Ok(result.Data));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error updating organization logo");
        }
    }

    /// <summary>
    /// Delete organization
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <returns>Deletion confirmation</returns>
    [HttpDelete]
    [RequireOrganizationAdmin]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOrganization()
    {
        try
        {
            var tenantContext = HttpContext.GetTenantContext()!;
            var result = await _organizationService.DeleteOrganizationAsync(tenantContext.OrganizationId);
            return HandleServiceResult(result, () => Ok(new SuccessResponse { Message = result.Message }));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error deleting organization");
        }
    }

    /// <summary>
    /// Activate organization
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <returns>Activation confirmation</returns>
    [HttpPost("activate")]
    [RequireOrganizationAdmin]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateOrganization()
    {
        try
        {
            var tenantContext = HttpContext.GetTenantContext()!;
            var result = await _organizationService.ActivateOrganizationAsync(tenantContext.OrganizationId);
            return HandleServiceResult(result, () => Ok(new SuccessResponse { Message = result.Message }));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error activating organization");
        }
    }

    /// <summary>
    /// Deactivate organization
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <returns>Deactivation confirmation</returns>
    [HttpPost("deactivate")]
    [RequireOrganizationAdmin]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateOrganization()
    {
        try
        {
            var tenantContext = HttpContext.GetTenantContext()!;
            var result = await _organizationService.DeactivateOrganizationAsync(tenantContext.OrganizationId);
            return HandleServiceResult(result, () => Ok(new SuccessResponse { Message = result.Message }));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error deactivating organization");
        }
    }

    /// <summary>
    /// Get organization with users
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <returns>Organization details with user information</returns>
    [HttpGet("users")]
    [RequireCanManageUsers]
    [ProducesResponseType(typeof(OrganizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrganizationWithUsers()
    {
        try
        {
            var tenantContext = HttpContext.GetTenantContext()!;
            var result = await _organizationService.GetOrganizationWithUsersAsync(tenantContext.OrganizationId);
            return HandleServiceResult(result, () => Ok(result.Data));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error retrieving organization with users");
        }
    }

    /// <summary>
    /// Get organization with subscriptions
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <returns>Organization details with subscription information</returns>
    [HttpGet("subscriptions")]
    [RequireOrganizationAdmin]
    [ProducesResponseType(typeof(OrganizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrganizationWithSubscriptions()
    {
        try
        {
            var tenantContext = HttpContext.GetTenantContext()!;
            var result = await _organizationService.GetOrganizationWithSubscriptionsAsync(tenantContext.OrganizationId);
            return HandleServiceResult(result, () => Ok(result.Data));
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error retrieving organization with subscriptions");
        }
    }
}
