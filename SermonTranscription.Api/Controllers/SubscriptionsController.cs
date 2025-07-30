using Microsoft.AspNetCore.Mvc;
using SermonTranscription.Api.Authorization;
using SermonTranscription.Api.Middleware;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Interfaces;

namespace SermonTranscription.Api.Controllers;

/// <summary>
/// Subscription management controller for plan management and usage tracking
/// </summary>
[Route("api/v{version:apiVersion}/subscriptions")]
[ApiVersion("1.0")]
public class SubscriptionsController : BaseAuthenticatedApiController
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(ISubscriptionService subscriptionService, ILogger<SubscriptionsController> logger)
        : base(logger)
    {
        _subscriptionService = subscriptionService;
    }

    /// <summary>
    /// Get available subscription plans
    /// </summary>
    /// <returns>List of available subscription plans</returns>
    [HttpGet("plans")]
    [PublicEndpoint]
    [ProducesResponseType(typeof(ApiSuccessResponse<IEnumerable<SubscriptionPlanResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAvailablePlans()
    {
        var result = await _subscriptionService.GetAvailablePlansAsync(HttpContext.RequestAborted);
        return HandleServiceResult(result, () => SuccessResponse(result.Data, "Available plans retrieved successfully"));
    }

    /// <summary>
    /// Get current organization subscription
    /// </summary>
    /// <returns>Current subscription details</returns>
    [HttpGet("current")]
    [ProducesResponseType(typeof(ApiSuccessResponse<SubscriptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentSubscription()
    {
        var tenantContext = HttpContext.GetTenantContext()!;
        var result = await _subscriptionService.GetCurrentSubscriptionAsync(tenantContext.OrganizationId, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => SuccessResponse(result.Data, "Current subscription retrieved successfully"));
    }

    /// <summary>
    /// Get organization subscription history with pagination
    /// </summary>
    /// <param name="request">Pagination and filtering parameters</param>
    /// <returns>Paginated list of organization subscriptions</returns>
    [HttpGet("history")]
    [ProducesResponseType(typeof(ApiSuccessResponse<SubscriptionHistoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscriptionHistory([FromQuery] SubscriptionHistoryRequest request)
    {
        var tenantContext = HttpContext.GetTenantContext()!;
        var result = await _subscriptionService.GetSubscriptionHistoryAsync(tenantContext.OrganizationId, request, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => SuccessResponse(result.Data, "Subscription history retrieved successfully"));
    }

    /// <summary>
    /// Create new subscription for organization
    /// </summary>
    /// <param name="request">Subscription creation request</param>
    /// <returns>Created subscription details</returns>
    [HttpPost]
    [RequireOrganizationAdmin]
    [ProducesResponseType(typeof(ApiSuccessResponse<SubscriptionResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest request)
    {
        _logger.LogInformation("Received request with Plan: {Plan}", request.Plan);
        var tenantContext = HttpContext.GetTenantContext()!;
        var result = await _subscriptionService.CreateSubscriptionAsync(tenantContext.OrganizationId, request.Plan, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => CreatedResponse(result.Data, "Subscription created successfully"));
    }

    /// <summary>
    /// Change subscription plan
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="request">Plan change request</param>
    /// <returns>Updated subscription details</returns>
    [HttpPut("{subscriptionId:guid}/plan")]
    [RequireOrganizationAdmin]
    [ProducesResponseType(typeof(ApiSuccessResponse<SubscriptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeSubscriptionPlan(Guid subscriptionId, [FromBody] ChangeSubscriptionPlanRequest request)
    {
        var result = await _subscriptionService.ChangeSubscriptionPlanAsync(subscriptionId, request.NewPlan, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => SuccessResponse(result.Data, "Subscription plan changed successfully"));
    }

    /// <summary>
    /// Cancel subscription
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <returns>Cancelled subscription details</returns>
    [HttpPost("{subscriptionId:guid}/cancel")]
    [RequireOrganizationAdmin]
    [ProducesResponseType(typeof(ApiSuccessResponse<SubscriptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelSubscription(Guid subscriptionId)
    {
        var result = await _subscriptionService.CancelSubscriptionAsync(subscriptionId, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => SuccessResponse(result.Data, "Subscription cancelled successfully"));
    }

    /// <summary>
    /// Reactivate subscription
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <returns>Reactivated subscription details</returns>
    [HttpPost("{subscriptionId:guid}/reactivate")]
    [RequireOrganizationAdmin]
    [ProducesResponseType(typeof(ApiSuccessResponse<SubscriptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReactivateSubscription(Guid subscriptionId)
    {
        var result = await _subscriptionService.ReactivateSubscriptionAsync(subscriptionId, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => SuccessResponse(result.Data, "Subscription reactivated successfully"));
    }

    /// <summary>
    /// Get subscription usage analytics
    /// </summary>
    /// <returns>Usage analytics for current subscription</returns>
    [HttpGet("usage")]
    [ProducesResponseType(typeof(ApiSuccessResponse<SubscriptionUsageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUsageAnalytics()
    {
        var tenantContext = HttpContext.GetTenantContext()!;
        var result = await _subscriptionService.GetUsageAnalyticsAsync(tenantContext.OrganizationId, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => SuccessResponse(result.Data, "Usage analytics retrieved successfully"));
    }

    /// <summary>
    /// Check if organization can use transcription minutes
    /// </summary>
    /// <param name="minutes">Minutes to check</param>
    /// <returns>Whether usage is allowed</returns>
    [HttpGet("can-use")]
    [ProducesResponseType(typeof(ApiSuccessResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CanUseTranscriptionMinutes([FromQuery] int minutes)
    {
        var tenantContext = HttpContext.GetTenantContext()!;
        var result = await _subscriptionService.CanUseTranscriptionMinutesAsync(tenantContext.OrganizationId, minutes, HttpContext.RequestAborted);
        return HandleServiceResult(result, () => SuccessResponse(result.Data, "Transcription minutes check completed"));
    }
}
