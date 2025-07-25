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
    [ProducesResponseType(typeof(IEnumerable<SubscriptionPlanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAvailablePlans()
    {
        try
        {
            var plans = await _subscriptionService.GetAvailablePlansAsync(HttpContext.RequestAborted);
            return Ok(plans);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Error retrieving available subscription plans");
        }
    }

    /// <summary>
    /// Get current subscription for the authenticated user's organization
    /// </summary>
    [HttpGet("current")]
    [ProducesResponseType(typeof(SubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCurrentSubscription()
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
                    Errors = ["Subscription not found"]
                });
            }

            return Ok(subscription);
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error retrieving current subscription for organization {HttpContext.GetTenantContext()?.OrganizationId}");
        }
    }

    /// <summary>
    /// Get all subscriptions for the authenticated user's organization
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(IEnumerable<SubscriptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSubscriptionHistory()
    {
        try
        {
            var tenantContext = HttpContext.GetTenantContext()!;
            var subscriptions = await _subscriptionService.GetOrganizationSubscriptionsAsync(tenantContext.OrganizationId, HttpContext.RequestAborted);
            return Ok(subscriptions);
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error retrieving subscription history for organization {HttpContext.GetTenantContext()?.OrganizationId}");
        }
    }

    /// <summary>
    /// Create a new subscription for the authenticated user's organization
    /// </summary>
    [HttpPost]
    [RequireOrganizationAdmin]
    [ProducesResponseType(typeof(SubscriptionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest request)
    {
        try
        {
            var validationResult = ValidateModelState();
            if (validationResult != null) return validationResult;

            var tenantContext = HttpContext.GetTenantContext()!;
            var subscription = await _subscriptionService.CreateSubscriptionAsync(tenantContext.OrganizationId, request.Plan, HttpContext.RequestAborted);

            return CreatedAtAction(nameof(GetCurrentSubscription), new { }, subscription);
        }
        catch (Exception ex) when (ex.Message.Contains("already has an active subscription"))
        {
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                Errors = [ex.Message]
            });
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error creating subscription for organization {HttpContext.GetTenantContext()?.OrganizationId}");
        }
    }

    /// <summary>
    /// Change the plan of an existing subscription
    /// </summary>
    [HttpPut("{subscriptionId}/plan")]
    [RequireOrganizationAdmin]
    [ProducesResponseType(typeof(SubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ChangeSubscriptionPlan(Guid subscriptionId, [FromBody] ChangeSubscriptionPlanRequest request)
    {
        try
        {
            var validationResult = ValidateModelState();
            if (validationResult != null) return validationResult;

            var subscription = await _subscriptionService.ChangeSubscriptionPlanAsync(subscriptionId, request.NewPlan, HttpContext.RequestAborted);
            return Ok(subscription);
        }
        catch (Exception ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new ErrorResponse
            {
                Message = ex.Message,
                Errors = [ex.Message]
            });
        }
        catch (Exception ex) when (ex.Message.Contains("inactive") || ex.Message.Contains("already"))
        {
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                Errors = [ex.Message]
            });
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error changing subscription plan {subscriptionId} to {request.NewPlan}");
        }
    }

    /// <summary>
    /// Cancel a subscription
    /// </summary>
    [HttpPost("{subscriptionId}/cancel")]
    [RequireOrganizationAdmin]
    [ProducesResponseType(typeof(SubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelSubscription(Guid subscriptionId)
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
                Errors = [ex.Message]
            });
        }
        catch (Exception ex) when (ex.Message.Contains("already cancelled"))
        {
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                Errors = [ex.Message]
            });
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error cancelling subscription {subscriptionId}");
        }
    }

    /// <summary>
    /// Reactivate a cancelled or suspended subscription
    /// </summary>
    [HttpPost("{subscriptionId}/reactivate")]
    [RequireOrganizationAdmin]
    [ProducesResponseType(typeof(SubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReactivateSubscription(Guid subscriptionId)
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
                Errors = [ex.Message]
            });
        }
        catch (Exception ex) when (ex.Message.Contains("already active"))
        {
            return BadRequest(new ErrorResponse
            {
                Message = ex.Message,
                Errors = [ex.Message]
            });
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error reactivating subscription {subscriptionId}");
        }
    }

    /// <summary>
    /// Get subscription usage analytics for the authenticated user's organization
    /// </summary>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(SubscriptionUsageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUsageAnalytics()
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
                Errors = [ex.Message]
            });
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error retrieving usage analytics for organization {HttpContext.GetTenantContext()?.OrganizationId}");
        }
    }

    /// <summary>
    /// Check if the organization can use transcription minutes based on subscription limits
    /// </summary>
    [HttpGet("limits/transcription")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckTranscriptionLimit([FromQuery] int minutes)
    {
        try
        {
            var tenantContext = HttpContext.GetTenantContext()!;
            var canUseMinutes = await _subscriptionService.CanUseTranscriptionMinutesAsync(tenantContext.OrganizationId, minutes, HttpContext.RequestAborted);

            return Ok(new { CanUseMinutes = canUseMinutes, RequestedMinutes = minutes });
        }
        catch (Exception ex)
        {
            return HandleException(ex, $"Error checking transcription limit for organization {HttpContext.GetTenantContext()?.OrganizationId}");
        }
    }
}
