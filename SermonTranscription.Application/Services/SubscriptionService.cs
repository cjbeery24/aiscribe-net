using AutoMapper;
using Microsoft.Extensions.Logging;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Interfaces;
using SermonTranscription.Application.Common;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Exceptions;
using SermonTranscription.Domain.Interfaces;

namespace SermonTranscription.Application.Services;

/// <summary>
/// Service for managing subscription plans and tier-based feature access
/// </summary>
public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        ISubscriptionRepository subscriptionRepository,
        IOrganizationRepository organizationRepository,
        IMapper mapper,
        ILogger<SubscriptionService> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _organizationRepository = organizationRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get the current active subscription for an organization
    /// </summary>
    public async Task<ServiceResult<SubscriptionResponse?>> GetCurrentSubscriptionAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetActiveByOrganizationAsync(organizationId, cancellationToken);
            var response = subscription != null ? _mapper.Map<SubscriptionResponse>(subscription) : null;
            return ServiceResult<SubscriptionResponse?>.Success(response, "Current subscription retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current subscription for organization {OrganizationId}", organizationId);
            return ServiceResult<SubscriptionResponse?>.Failure("An error occurred while retrieving the current subscription", ErrorCode.InternalError);
        }
    }

    /// <summary>
    /// Get all subscriptions for an organization
    /// </summary>
    public async Task<ServiceResult<IEnumerable<SubscriptionResponse>>> GetOrganizationSubscriptionsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var subscriptions = await _subscriptionRepository.GetByOrganizationAsync(organizationId, cancellationToken);
            var response = _mapper.Map<IEnumerable<SubscriptionResponse>>(subscriptions);
            return ServiceResult<IEnumerable<SubscriptionResponse>>.Success(response, "Organization subscriptions retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscriptions for organization {OrganizationId}", organizationId);
            return ServiceResult<IEnumerable<SubscriptionResponse>>.Failure("An error occurred while retrieving organization subscriptions", ErrorCode.InternalError);
        }
    }

    /// <summary>
    /// Create a new subscription for an organization
    /// </summary>
    public async Task<ServiceResult<SubscriptionResponse>> CreateSubscriptionAsync(Guid organizationId, SubscriptionPlan plan, CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify organization exists
            var organization = await _organizationRepository.GetByIdAsync(organizationId, cancellationToken);
            if (organization == null)
            {
                return ServiceResult<SubscriptionResponse>.Failure($"Organization with ID {organizationId} not found", ErrorCode.NotFound);
            }

            // Check if organization already has an active subscription
            var existingSubscription = await _subscriptionRepository.GetActiveByOrganizationAsync(organizationId, cancellationToken);
            if (existingSubscription != null)
            {
                return ServiceResult<SubscriptionResponse>.Failure("Organization already has an active subscription", ErrorCode.Conflict);
            }

            // Create new subscription
            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                Plan = plan,
                Status = SubscriptionStatus.Active,
                StartDate = DateTime.UtcNow,
                UsageResetDate = DateTime.UtcNow.AddMonths(1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Set plan-specific limits
            subscription.UpdatePlanLimits();

            await _subscriptionRepository.AddAsync(subscription, cancellationToken);
            await _subscriptionRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created subscription {SubscriptionId} for organization {OrganizationId} with plan {Plan}",
                subscription.Id, organizationId, plan);

            var response = _mapper.Map<SubscriptionResponse>(subscription);
            return ServiceResult<SubscriptionResponse>.Success(response, "Subscription created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription for organization {OrganizationId} with plan {Plan}", organizationId, plan);
            return ServiceResult<SubscriptionResponse>.Failure("An error occurred while creating the subscription", ErrorCode.InternalError);
        }
    }

    /// <summary>
    /// Upgrade or downgrade a subscription plan
    /// </summary>
    public async Task<ServiceResult<SubscriptionResponse>> ChangeSubscriptionPlanAsync(Guid subscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken = default)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken);
            if (subscription == null)
            {
                return ServiceResult<SubscriptionResponse>.Failure($"Subscription with ID {subscriptionId} not found", ErrorCode.NotFound);
            }

            if (!subscription.IsActive)
            {
                return ServiceResult<SubscriptionResponse>.Failure("Cannot change plan for inactive subscription", ErrorCode.Forbidden);
            }

            var oldPlan = subscription.Plan;

            if (newPlan != subscription.Plan)
            {
                subscription.ChangePlan(newPlan);
                if (newPlan > oldPlan)
                {
                    _logger.LogInformation("Upgraded subscription {SubscriptionId} from {OldPlan} to {NewPlan}",
                        subscriptionId, oldPlan, newPlan);
                }
                else
                {
                    _logger.LogInformation("Downgraded subscription {SubscriptionId} from {OldPlan} to {NewPlan}",
                        subscriptionId, oldPlan, newPlan);
                }
            }
            else
            {
                _logger.LogInformation("Subscription {SubscriptionId} plan unchanged: {Plan}", subscriptionId, newPlan);
            }

            await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);
            await _subscriptionRepository.SaveChangesAsync(cancellationToken);

            var response = _mapper.Map<SubscriptionResponse>(subscription);
            return ServiceResult<SubscriptionResponse>.Success(response, "Subscription plan changed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing subscription plan for subscription {SubscriptionId} to {NewPlan}", subscriptionId, newPlan);
            return ServiceResult<SubscriptionResponse>.Failure("An error occurred while changing the subscription plan", ErrorCode.InternalError);
        }
    }

    /// <summary>
    /// Cancel a subscription
    /// </summary>
    public async Task<ServiceResult<SubscriptionResponse>> CancelSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken);
            if (subscription == null)
            {
                return ServiceResult<SubscriptionResponse>.Failure($"Subscription with ID {subscriptionId} not found", ErrorCode.NotFound);
            }

            if (subscription.IsCancelled)
            {
                return ServiceResult<SubscriptionResponse>.Failure("Subscription is already cancelled", ErrorCode.Conflict);
            }

            subscription.Cancel();
            await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);
            await _subscriptionRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Cancelled subscription {SubscriptionId}", subscriptionId);

            var response = _mapper.Map<SubscriptionResponse>(subscription);
            return ServiceResult<SubscriptionResponse>.Success(response, "Subscription cancelled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription {SubscriptionId}", subscriptionId);
            return ServiceResult<SubscriptionResponse>.Failure("An error occurred while cancelling the subscription", ErrorCode.InternalError);
        }
    }

    /// <summary>
    /// Reactivate a cancelled or suspended subscription
    /// </summary>
    public async Task<ServiceResult<SubscriptionResponse>> ReactivateSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken);
            if (subscription == null)
            {
                return ServiceResult<SubscriptionResponse>.Failure($"Subscription with ID {subscriptionId} not found", ErrorCode.NotFound);
            }

            if (subscription.IsActive)
            {
                return ServiceResult<SubscriptionResponse>.Failure("Subscription is already active", ErrorCode.Conflict);
            }

            subscription.Reactivate();
            await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);
            await _subscriptionRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Reactivated subscription {SubscriptionId}", subscriptionId);

            var response = _mapper.Map<SubscriptionResponse>(subscription);
            return ServiceResult<SubscriptionResponse>.Success(response, "Subscription reactivated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating subscription {SubscriptionId}", subscriptionId);
            return ServiceResult<SubscriptionResponse>.Failure("An error occurred while reactivating the subscription", ErrorCode.InternalError);
        }
    }

    /// <summary>
    /// Track usage of transcription minutes
    /// </summary>
    public async Task<ServiceResult<SubscriptionResponse>> TrackTranscriptionUsageAsync(Guid subscriptionId, int minutesUsed, CancellationToken cancellationToken = default)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken);
            if (subscription == null)
            {
                return ServiceResult<SubscriptionResponse>.Failure($"Subscription with ID {subscriptionId} not found", ErrorCode.NotFound);
            }

            if (!subscription.IsActive)
            {
                return ServiceResult<SubscriptionResponse>.Failure("Cannot track usage for inactive subscription", ErrorCode.Forbidden);
            }

            if (minutesUsed < 0)
            {
                return ServiceResult<SubscriptionResponse>.Failure("Minutes used cannot be negative", ErrorCode.ValidationError, "minutesUsed");
            }

            if (!subscription.CanUseTranscriptionMinutes(minutesUsed))
            {
                return ServiceResult<SubscriptionResponse>.Failure("Insufficient transcription minutes remaining.", ErrorCode.ValidationError, "minutesUsed");
            }

            subscription.UseTranscriptionMinutes(minutesUsed);
            await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);
            await _subscriptionRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Tracked {MinutesUsed} minutes usage for subscription {SubscriptionId}", minutesUsed, subscriptionId);

            var response = _mapper.Map<SubscriptionResponse>(subscription);
            return ServiceResult<SubscriptionResponse>.Success(response, "Usage tracked successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking usage for subscription {SubscriptionId}", subscriptionId);
            return ServiceResult<SubscriptionResponse>.Failure("An error occurred while tracking usage", ErrorCode.InternalError);
        }
    }

    /// <summary>
    /// Reset monthly usage for a subscription
    /// </summary>
    public async Task<ServiceResult<SubscriptionResponse>> ResetMonthlyUsageAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken);
            if (subscription == null)
            {
                return ServiceResult<SubscriptionResponse>.Failure($"Subscription with ID {subscriptionId} not found", ErrorCode.NotFound);
            }

            subscription.ResetUsage();
            await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);
            await _subscriptionRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Reset monthly usage for subscription {SubscriptionId}", subscriptionId);

            var response = _mapper.Map<SubscriptionResponse>(subscription);
            return ServiceResult<SubscriptionResponse>.Success(response, "Monthly usage reset successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting monthly usage for subscription {SubscriptionId}", subscriptionId);
            return ServiceResult<SubscriptionResponse>.Failure("An error occurred while resetting monthly usage", ErrorCode.InternalError);
        }
    }

    /// <summary>
    /// Check if an organization can use transcription minutes based on subscription limits
    /// </summary>
    public async Task<ServiceResult<bool>> CanUseTranscriptionMinutesAsync(Guid organizationId, int minutes, CancellationToken cancellationToken = default)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetActiveByOrganizationAsync(organizationId, cancellationToken);
            if (subscription == null)
            {
                return ServiceResult<bool>.Failure("No active subscription found for organization", ErrorCode.NotFound);
            }

            var canUse = subscription.CanUseTranscriptionMinutes(minutes);
            return ServiceResult<bool>.Success(canUse, "Usage check completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking transcription minutes usage for organization {OrganizationId}", organizationId);
            return ServiceResult<bool>.Failure("An error occurred while checking usage", ErrorCode.InternalError);
        }
    }

    /// <summary>
    /// Get subscription usage analytics for an organization
    /// </summary>
    public async Task<ServiceResult<SubscriptionUsageResponse>> GetUsageAnalyticsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetActiveByOrganizationAsync(organizationId, cancellationToken);
            if (subscription == null)
            {
                return ServiceResult<SubscriptionUsageResponse>.Failure("No active subscription found for organization", ErrorCode.NotFound);
            }

            var response = new SubscriptionUsageResponse
            {
                OrganizationId = organizationId,
                CurrentPlan = subscription.Plan,
                PlanName = subscription.Plan.ToString(),
                MonthlyLimit = subscription.MaxTranscriptionMinutes,
                MinutesUsed = subscription.TranscriptionMinutesUsed,
                MinutesRemaining = subscription.RemainingTranscriptionMinutes,
                TotalUsage = subscription.TranscriptionMinutesUsed,
                UsagePercentage = subscription.MaxTranscriptionMinutes > 0
                ? (decimal)subscription.TranscriptionMinutesUsed / subscription.MaxTranscriptionMinutes * 100
                : 0,
                UsageResetDate = subscription.UsageResetDate,
                IsNearLimit = subscription.RemainingTranscriptionMinutes <= 120,
            };

            return ServiceResult<SubscriptionUsageResponse>.Success(response, "Usage analytics retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving usage analytics for organization {OrganizationId}", organizationId);
            return ServiceResult<SubscriptionUsageResponse>.Failure("An error occurred while retrieving usage analytics", ErrorCode.InternalError);
        }
    }

    /// <summary>
    /// Get available subscription plans with their features and pricing
    /// </summary>
    public Task<ServiceResult<IEnumerable<SubscriptionPlanResponse>>> GetAvailablePlansAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var plans = Enum.GetValues<SubscriptionPlan>()
                .Select(plan => new SubscriptionPlanResponse
                {
                    Plan = plan,
                    PlanName = GetPlanDisplayName(plan),
                    MonthlyPrice = GetPlanPrice(plan),
                    YearlyPrice = GetPlanPrice(plan) * 12,
                    MaxTranscriptionMinutes = GetPlanMinutesLimit(plan),
                    CanExportTranscriptions = plan == SubscriptionPlan.Basic || plan == SubscriptionPlan.Professional || plan == SubscriptionPlan.Enterprise,
                    HasRealtimeTranscription = plan == SubscriptionPlan.Professional || plan == SubscriptionPlan.Enterprise,
                    HasPrioritySupport = plan == SubscriptionPlan.Professional || plan == SubscriptionPlan.Enterprise,
                    Features = GetPlanFeatures(plan)
                })
                .ToList();

            return Task.FromResult(ServiceResult<IEnumerable<SubscriptionPlanResponse>>.Success(plans, "Available plans retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available subscription plans");
            return Task.FromResult(ServiceResult<IEnumerable<SubscriptionPlanResponse>>.Failure("An error occurred while retrieving available plans", ErrorCode.InternalError));
        }
    }

    private static string GetPlanDisplayName(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Basic => "Basic",
        SubscriptionPlan.Professional => "Professional",
        SubscriptionPlan.Enterprise => "Enterprise",
        _ => plan.ToString()
    };

    private static string GetPlanDescription(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Basic => "Ideal for growing churches with regular sermon transcription needs",
        SubscriptionPlan.Professional => "Advanced features for larger churches and organizations with high transcription volume",
        SubscriptionPlan.Enterprise => "Custom solutions for large organizations with specific requirements",
        _ => "Subscription plan"
    };

    private static decimal GetPlanPrice(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Basic => 29.99m,
        SubscriptionPlan.Professional => 79.99m,
        SubscriptionPlan.Enterprise => 199.99m,
        _ => 0.00m
    };

    private static int GetPlanMinutesLimit(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Basic => 300,
        SubscriptionPlan.Professional => 1000,
        SubscriptionPlan.Enterprise => 5000,
        _ => 0
    };

    private static List<string> GetPlanFeatures(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Basic => new List<string>
        {
            "300 minutes of transcription per month",
            "Improved transcription accuracy",
            "Email support",
            "Export to common formats"
        },
        SubscriptionPlan.Professional => new List<string>
        {
            "1000 minutes of transcription per month",
            "High transcription accuracy",
            "Priority support",
            "Advanced export options",
            "Custom vocabulary training",
            "Analytics dashboard"
        },
        SubscriptionPlan.Enterprise => new List<string>
        {
            "5000 minutes of transcription per month",
            "Highest transcription accuracy",
            "Dedicated support",
            "Custom integrations",
            "Advanced analytics",
            "Custom vocabulary training",
            "API access",
            "Custom branding"
        },
        _ => new List<string>()
    };
}
