using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SermonTranscription.Api.Authorization;
using SermonTranscription.Api.Middleware;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Interfaces;

namespace SermonTranscription.Api.Controllers;

/// <summary>
/// Controller for managing subscription plans and tier-based feature access
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
    /// Get available subscription plans with features and pricing
    /// </summary>
    [HttpGet("plans")]
    [PublicEndpoint]
    public async Task<ActionResult<IEnumerable<SubscriptionPlanResponse>>> GetAvailablePlans()
    {
        try
        {
            var plans = await _subscriptionService.GetAvailablePlansAsync(HttpContext.RequestAborted);
            return Ok(plans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available subscription plans");
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while retrieving subscription plans",
                Errors = new[] { "Internal server error" }
            });
        }
    }

    /// <summary>
    /// Get current subscription for the authenticated user's organization
    /// </summary>
    [HttpGet("current")]
    public async Task<ActionResult<SubscriptionResponse>> GetCurrentSubscription()
    {
        try
        {
            var tenantContext = HttpContext.GetTenantContext()!;
            var subscription = await _subscriptionService.GetCurrentSubscriptionAsync(tenantContext.OrganizationId, HttpContext.RequestAborted);

            if (subscription == null)
            {
                return NotFound(new ErrorResponse
                {
                    Message = "No active subscription found for this organization",
                    Errors = new[] { "Subscription not found" }
                });
            }

            return Ok(subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current subscription for organization {OrganizationId}", HttpContext.GetTenantContext()?.OrganizationId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while retrieving the subscription",
                Errors = new[] { "Internal server error" }
            });
        }
    }

    /// <summary>
    /// Get all subscriptions for the authenticated user's organization
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<SubscriptionResponse>>> GetSubscriptionHistory()
    {
        try
        {
            var tenantContext = HttpContext.GetTenantContext()!;
            var subscriptions = await _subscriptionService.GetOrganizationSubscriptionsAsync(tenantContext.OrganizationId, HttpContext.RequestAborted);
            return Ok(subscriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription history for organization {OrganizationId}", HttpContext.GetTenantContext()?.OrganizationId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while retrieving subscription history",
                Errors = new[] { "Internal server error" }
            });
        }
    }

    /// <summary>
    /// Create a new subscription for the authenticated user's organization
    /// </summary>
    [HttpPost]
    [RequireOrganizationAdmin]
    public async Task<ActionResult<SubscriptionResponse>> CreateSubscription(
        [FromBody] CreateSubscriptionRequest request)
    {
        try
        {
            var tenantContext = HttpContext.GetTenantContext()!;
            var subscription = await _subscriptionService.CreateSubscriptionAsync(tenantContext.OrganizationId, request.Plan, HttpContext.RequestAborted);

            return CreatedAtAction(nameof(GetCurrentSubscription), new { }, subscription);
        }
        catch (Exception ex) when (ex.Message.Contains("already has an active subscription"))
        {
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                Errors = new[] { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription for organization {OrganizationId}", HttpContext.GetTenantContext()?.OrganizationId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while creating the subscription",
                Errors = new[] { "Internal server error" }
            });
        }
    }

    /// <summary>
    /// Change the plan of an existing subscription
    /// </summary>
    [HttpPut("{subscriptionId}/plan")]
    [RequireOrganizationAdmin]
    public async Task<ActionResult<SubscriptionResponse>> ChangeSubscriptionPlan(
        Guid subscriptionId,
        [FromBody] ChangeSubscriptionPlanRequest request)
    {
        try
        {
            var subscription = await _subscriptionService.ChangeSubscriptionPlanAsync(subscriptionId, request.NewPlan, HttpContext.RequestAborted);
            return Ok(subscription);
        }
        catch (Exception ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new ErrorResponse
            {
                Message = ex.Message,
                Errors = new[] { ex.Message }
            });
        }
        catch (Exception ex) when (ex.Message.Contains("inactive") || ex.Message.Contains("already"))
        {
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                Errors = new[] { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing subscription plan {SubscriptionId} to {NewPlan}", subscriptionId, request.NewPlan);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while changing the subscription plan",
                Errors = new[] { "Internal server error" }
            });
        }
    }

    /// <summary>
    /// Cancel a subscription
    /// </summary>
    [HttpPost("{subscriptionId}/cancel")]
    [RequireOrganizationAdmin]
    public async Task<ActionResult<SubscriptionResponse>> CancelSubscription(
        Guid subscriptionId)
    {
        try
        {
            var subscription = await _subscriptionService.CancelSubscriptionAsync(subscriptionId, HttpContext.RequestAborted);
            return Ok(subscription);
        }
        catch (Exception ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new ErrorResponse
            {
                Message = ex.Message,
                Errors = new[] { ex.Message }
            });
        }
        catch (Exception ex) when (ex.Message.Contains("already cancelled"))
        {
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                Errors = new[] { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription {SubscriptionId}", subscriptionId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while cancelling the subscription",
                Errors = new[] { "Internal server error" }
            });
        }
    }

    /// <summary>
    /// Reactivate a cancelled or suspended subscription
    /// </summary>
    [HttpPost("{subscriptionId}/reactivate")]
    [RequireOrganizationAdmin]
    public async Task<ActionResult<SubscriptionResponse>> ReactivateSubscription(
        Guid subscriptionId)
    {
        try
        {
            var subscription = await _subscriptionService.ReactivateSubscriptionAsync(subscriptionId, HttpContext.RequestAborted);
            return Ok(subscription);
        }
        catch (Exception ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new ErrorResponse
            {
                Message = ex.Message,
                Errors = new[] { ex.Message }
            });
        }
        catch (Exception ex) when (ex.Message.Contains("already active"))
        {
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                Errors = new[] { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating subscription {SubscriptionId}", subscriptionId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while reactivating the subscription",
                Errors = new[] { "Internal server error" }
            });
        }
    }

    /// <summary>
    /// Get subscription usage analytics for the authenticated user's organization
    /// </summary>
    [HttpGet("analytics")]
    public async Task<ActionResult<SubscriptionUsageResponse>> GetUsageAnalytics()
    {
        try
        {
            var tenantContext = HttpContext.GetTenantContext()!;
            var analytics = await _subscriptionService.GetUsageAnalyticsAsync(tenantContext.OrganizationId, HttpContext.RequestAborted);
            return Ok(analytics);
        }
        catch (Exception ex) when (ex.Message.Contains("No active subscription"))
        {
            return NotFound(new ErrorResponse
            {
                Message = ex.Message,
                Errors = new[] { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving usage analytics for organization {OrganizationId}", HttpContext.GetTenantContext()?.OrganizationId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while retrieving usage analytics",
                Errors = new[] { "Internal server error" }
            });
        }
    }

    /// <summary>
    /// Check if the organization can use transcription minutes based on subscription limits
    /// </summary>
    [HttpGet("limits/transcription")]
    public async Task<ActionResult<object>> CheckTranscriptionLimit(
        [FromQuery] int minutes)
    {
        try
        {
            var tenantContext = HttpContext.GetTenantContext()!;
            var canUseMinutes = await _subscriptionService.CanUseTranscriptionMinutesAsync(tenantContext.OrganizationId, minutes, HttpContext.RequestAborted);

            return Ok(new { CanUseMinutes = canUseMinutes, RequestedMinutes = minutes });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking transcription limit for organization {OrganizationId}", HttpContext.GetTenantContext()?.OrganizationId);
            return StatusCode(500, new ErrorResponse
            {
                Message = "An error occurred while checking transcription limits",
                Errors = new[] { "Internal server error" }
            });
        }
    }


}
