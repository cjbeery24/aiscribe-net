using Microsoft.AspNetCore.Mvc;
using SermonTranscription.Api.Authorization;
using SermonTranscription.Api.Middleware;
using SermonTranscription.Application.DTOs;
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
    [ProducesResponseType(typeof(ApiResponse<OrganizationResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return UnauthorizedError("User not authenticated");
        }

        var result = await _organizationService.CreateOrganizationAsync(request, userId.Value, HttpContext.RequestAborted);
        return HandleServiceResult(result, () =>
            StatusCode(201, SuccessResponse(result.Data!, "Organization created successfully")));
    }

    /// <summary>
    /// Get organization by ID
    /// </summary>
    /// <returns>Organization details</returns>
    [HttpGet("load")]
    [ProducesResponseType(typeof(OrganizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LoadOrganization()
    {
        var tenantContext = HttpContext.GetTenantContext()!;
        var result = await _organizationService.GetOrganizationAsync(tenantContext.OrganizationId, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => Ok(result.Data));
    }

    /// <summary>
    /// Update organization details
    /// </summary>
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
        var tenantContext = HttpContext.GetTenantContext()!;
        var result = await _organizationService.UpdateOrganizationAsync(tenantContext.OrganizationId, request, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => Ok(result.Data));
    }

    /// <summary>
    /// Update organization settings
    /// </summary>
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
        var tenantContext = HttpContext.GetTenantContext()!;
        var result = await _organizationService.UpdateOrganizationSettingsAsync(tenantContext.OrganizationId, request, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => Ok(result.Data));
    }

    /// <summary>
    /// Update organization logo
    /// </summary>
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
        var tenantContext = HttpContext.GetTenantContext()!;
        var result = await _organizationService.UpdateOrganizationLogoAsync(tenantContext.OrganizationId, request, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => Ok(result.Data));
    }

    /// <summary>
    /// Activate organization
    /// </summary>
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
        var tenantContext = HttpContext.GetTenantContext()!;
        var result = await _organizationService.ActivateOrganizationAsync(tenantContext.OrganizationId, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => SuccessResponse("Organization activated successfully"));
    }

    /// <summary>
    /// Deactivate organization
    /// </summary>
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
        var tenantContext = HttpContext.GetTenantContext()!;
        var result = await _organizationService.DeactivateOrganizationAsync(tenantContext.OrganizationId, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => SuccessResponse("Organization deactivated successfully"));
    }

    /// <summary>
    /// Get organization with users
    /// </summary>
    /// <returns>Organization details with user list</returns>
    [HttpGet("users")]
    [RequireCanManageUsers]
    [ProducesResponseType(typeof(OrganizationWithUsersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrganizationWithUsers()
    {
        var tenantContext = HttpContext.GetTenantContext()!;
        var result = await _organizationService.GetOrganizationWithUsersAsync(tenantContext.OrganizationId, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => Ok(result.Data));
    }

    /// <summary>
    /// Get organization with subscriptions
    /// </summary>
    /// <returns>Organization details with subscription list</returns>
    [HttpGet("subscriptions")]
    [RequireOrganizationAdmin]
    [ProducesResponseType(typeof(OrganizationWithSubscriptionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrganizationWithSubscriptions()
    {
        var tenantContext = HttpContext.GetTenantContext()!;
        var result = await _organizationService.GetOrganizationWithSubscriptionsAsync(tenantContext.OrganizationId, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => Ok(result.Data));
    }

    /// <summary>
    /// Get organization dashboard data
    /// </summary>
    /// <returns>Organization dashboard information</returns>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(OrganizationDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrganizationDashboard()
    {
        var tenantContext = HttpContext.GetTenantContext()!;
        var result = await _organizationService.GetOrganizationDashboardAsync(tenantContext.OrganizationId, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => Ok(result.Data));
    }
}
