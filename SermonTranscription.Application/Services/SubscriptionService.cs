using AutoMapper;
using Microsoft.Extensions.Logging;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Interfaces;
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
    public async Task<SubscriptionResponse?> GetCurrentSubscriptionAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetActiveByOrganizationAsync(organizationId, cancellationToken);
        return subscription != null ? _mapper.Map<SubscriptionResponse>(subscription) : null;
    }

    /// <summary>
    /// Get all subscriptions for an organization
    /// </summary>
    public async Task<IEnumerable<SubscriptionResponse>> GetOrganizationSubscriptionsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var subscriptions = await _subscriptionRepository.GetByOrganizationAsync(organizationId, cancellationToken);
        return _mapper.Map<IEnumerable<SubscriptionResponse>>(subscriptions);
    }

    /// <summary>
    /// Create a new subscription for an organization
    /// </summary>
    public async Task<SubscriptionResponse> CreateSubscriptionAsync(Guid organizationId, SubscriptionPlan plan, CancellationToken cancellationToken = default)
    {
        // Verify organization exists
        var organization = await _organizationRepository.GetByIdAsync(organizationId, cancellationToken);
        if (organization == null)
        {
            throw new OrganizationDomainException($"Organization with ID {organizationId} not found");
        }

        // Check if organization already has an active subscription
        var existingSubscription = await _subscriptionRepository.GetActiveByOrganizationAsync(organizationId, cancellationToken);
        if (existingSubscription != null)
        {
            throw new OrganizationDomainException($"Organization already has an active subscription");
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

        return _mapper.Map<SubscriptionResponse>(subscription);
    }

    /// <summary>
    /// Upgrade or downgrade a subscription plan
    /// </summary>
    public async Task<SubscriptionResponse> ChangeSubscriptionPlanAsync(Guid subscriptionId, SubscriptionPlan newPlan, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken);
        if (subscription == null)
        {
            throw new SubscriptionDomainException($"Subscription with ID {subscriptionId} not found");
        }

        if (!subscription.IsActive)
        {
            throw new SubscriptionDomainException("Cannot change plan for inactive subscription");
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
            _logger.LogWarning("Attempted to change subscription {SubscriptionId} to same plan {Plan}",
                subscriptionId, newPlan);
            return _mapper.Map<SubscriptionResponse>(subscription);
        }

        await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);

        return _mapper.Map<SubscriptionResponse>(subscription);
    }

    /// <summary>
    /// Cancel a subscription
    /// </summary>
    public async Task<SubscriptionResponse> CancelSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken);
        if (subscription == null)
        {
            throw new SubscriptionDomainException($"Subscription with ID {subscriptionId} not found");
        }

        if (subscription.IsCancelled)
        {
            throw new SubscriptionDomainException("Subscription is already cancelled");
        }

        subscription.Cancel();
        await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);

        _logger.LogInformation("Cancelled subscription {SubscriptionId}", subscriptionId);

        return _mapper.Map<SubscriptionResponse>(subscription);
    }

    /// <summary>
    /// Reactivate a cancelled or suspended subscription
    /// </summary>
    public async Task<SubscriptionResponse> ReactivateSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken);
        if (subscription == null)
        {
            throw new SubscriptionDomainException($"Subscription with ID {subscriptionId} not found");
        }

        if (subscription.IsActive)
        {
            throw new SubscriptionDomainException("Subscription is already active");
        }

        subscription.Reactivate();
        await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);

        _logger.LogInformation("Reactivated subscription {SubscriptionId}", subscriptionId);

        return _mapper.Map<SubscriptionResponse>(subscription);
    }

    /// <summary>
    /// Track usage of transcription minutes
    /// </summary>
    public async Task<SubscriptionResponse> TrackTranscriptionUsageAsync(Guid subscriptionId, int minutesUsed, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken);
        if (subscription == null)
        {
            throw new SubscriptionDomainException($"Subscription with ID {subscriptionId} not found");
        }

        if (!subscription.IsActive)
        {
            throw new SubscriptionDomainException("Cannot track usage for inactive subscription");
        }

        if (!subscription.CanUseTranscriptionMinutes(minutesUsed))
        {
            throw new SubscriptionDomainException($"Insufficient transcription minutes remaining. Requested: {minutesUsed}, Available: {subscription.RemainingTranscriptionMinutes}");
        }

        subscription.UseTranscriptionMinutes(minutesUsed);
        await _subscriptionRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tracked {MinutesUsed} transcription minutes for subscription {SubscriptionId}. Remaining: {RemainingMinutes}",
            minutesUsed, subscriptionId, subscription.RemainingTranscriptionMinutes);

        return _mapper.Map<SubscriptionResponse>(subscription);
    }

    /// <summary>
    /// Reset monthly usage for a subscription
    /// </summary>
    public async Task<SubscriptionResponse> ResetMonthlyUsageAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, cancellationToken);
        if (subscription == null)
        {
            throw new SubscriptionDomainException($"Subscription with ID {subscriptionId} not found");
        }

        subscription.ResetUsage();
        await _subscriptionRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Reset monthly usage for subscription {SubscriptionId}", subscriptionId);

        return _mapper.Map<SubscriptionResponse>(subscription);
    }

    /// <summary>
    /// Check if an organization can use transcription minutes based on subscription limits
    /// </summary>
    public async Task<bool> CanUseTranscriptionMinutesAsync(Guid organizationId, int minutes, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetActiveByOrganizationAsync(organizationId, cancellationToken);
        return subscription?.CanUseTranscriptionMinutes(minutes) ?? false;
    }

    /// <summary>
    /// Get subscription usage analytics for an organization
    /// </summary>
    public async Task<SubscriptionUsageResponse> GetUsageAnalyticsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetActiveByOrganizationAsync(organizationId, cancellationToken);
        if (subscription == null)
        {
            throw new SubscriptionDomainException($"No active subscription found for organization {organizationId}");
        }

        var totalUsage = await _subscriptionRepository.GetTotalUsageMinutesAsync(organizationId, null, cancellationToken);

        return new SubscriptionUsageResponse
        {
            OrganizationId = organizationId,
            CurrentPlan = subscription.Plan,
            PlanName = subscription.Plan.ToString(),
            MonthlyLimit = subscription.MaxTranscriptionMinutes,
            MinutesUsed = subscription.TranscriptionMinutesUsed,
            MinutesRemaining = subscription.RemainingTranscriptionMinutes,
            TotalUsage = totalUsage,
            UsagePercentage = subscription.MaxTranscriptionMinutes > 0
                ? (decimal)subscription.TranscriptionMinutesUsed / subscription.MaxTranscriptionMinutes * 100
                : 0,
            UsageResetDate = subscription.UsageResetDate,
            IsNearLimit = subscription.RemainingTranscriptionMinutes <= 120 // 2 hours in minutes
        };
    }

    /// <summary>
    /// Get available subscription plans with their features and pricing
    /// </summary>
    public Task<IEnumerable<SubscriptionPlanResponse>> GetAvailablePlansAsync(CancellationToken cancellationToken = default)
    {
        var plans = new List<SubscriptionPlanResponse>();

        foreach (SubscriptionPlan plan in Enum.GetValues(typeof(SubscriptionPlan)))
        {
            var tempSubscription = new Subscription { Plan = plan };
            tempSubscription.UpdatePlanLimits();

            plans.Add(new SubscriptionPlanResponse
            {
                Plan = plan,
                PlanName = plan.ToString(),
                MonthlyPrice = tempSubscription.MonthlyPrice,
                YearlyPrice = tempSubscription.YearlyPrice,
                MaxTranscriptionMinutes = tempSubscription.MaxTranscriptionMinutes,
                CanExportTranscriptions = tempSubscription.CanExportTranscriptions,
                HasRealtimeTranscription = tempSubscription.HasRealtimeTranscription,
                HasPrioritySupport = tempSubscription.HasPrioritySupport,
                Features = GetPlanFeatures(plan)
            });
        }

        return Task.FromResult<IEnumerable<SubscriptionPlanResponse>>(plans);
    }

    private static List<string> GetPlanFeatures(SubscriptionPlan plan)
    {
        return plan switch
        {
            SubscriptionPlan.Basic => new List<string>
            {
                "Unlimited users",
                "6 hours of transcription per month",
                "Real-time transcription",
                "Export transcriptions",
                "Email support"
            },
            SubscriptionPlan.Professional => new List<string>
            {
                "Unlimited users",
                "10 hours of transcription per month",
                "Real-time transcription",
                "Export transcriptions",
                "Priority support",
                "Advanced analytics"
            },
            SubscriptionPlan.Enterprise => new List<string>
            {
                "Unlimited users",
                "14 hours of transcription per month",
                "Real-time transcription",
                "Export transcriptions",
                "Priority support",
                "Advanced analytics",
                "Custom integrations",
                "Dedicated account manager"
            },
            _ => new List<string>()
        };
    }
}
