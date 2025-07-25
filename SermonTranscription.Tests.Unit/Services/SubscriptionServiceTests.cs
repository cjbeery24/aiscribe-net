using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Services;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Exceptions;
using SermonTranscription.Domain.Interfaces;
using SermonTranscription.Tests.Unit.Common;
using Xunit;

namespace SermonTranscription.Tests.Unit.Services;

public class SubscriptionServiceTests : BaseUnitTest
{
    private readonly Mock<ISubscriptionRepository> _mockSubscriptionRepository;
    private readonly Mock<IOrganizationRepository> _mockOrganizationRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<SubscriptionService>> _mockLogger;
    private readonly SubscriptionService _subscriptionService;

    public SubscriptionServiceTests()
    {
        _mockSubscriptionRepository = new Mock<ISubscriptionRepository>();
        _mockOrganizationRepository = new Mock<IOrganizationRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<SubscriptionService>>();

        _subscriptionService = new SubscriptionService(
            _mockSubscriptionRepository.Object,
            _mockOrganizationRepository.Object,
            _mockMapper.Object,
            _mockLogger.Object);
    }

    #region GetCurrentSubscriptionAsync Tests

    [Fact]
    public async Task GetCurrentSubscriptionAsync_WithActiveSubscription_ShouldReturnSubscription()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.OrganizationId = organizationId;
        subscription.Status = SubscriptionStatus.Active;

        var expectedResponse = new SubscriptionResponse
        {
            Id = subscription.Id,
            OrganizationId = subscription.OrganizationId,
            Plan = subscription.Plan,
            Status = subscription.Status
        };

        _mockSubscriptionRepository
            .Setup(x => x.GetActiveByOrganizationAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(subscription);

        _mockMapper
            .Setup(x => x.Map<SubscriptionResponse>(subscription))
            .Returns(expectedResponse);

        // Act
        var result = await _subscriptionService.GetCurrentSubscriptionAsync(organizationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(subscription.Id, result.Id);
        Assert.Equal(subscription.OrganizationId, result.OrganizationId);
        Assert.Equal(subscription.Plan, result.Plan);
        Assert.Equal(subscription.Status, result.Status);
    }

    [Fact]
    public async Task GetCurrentSubscriptionAsync_WithNoActiveSubscription_ShouldReturnNull()
    {
        // Arrange
        var organizationId = Guid.NewGuid();

        _mockSubscriptionRepository
            .Setup(x => x.GetActiveByOrganizationAsync(organizationId, CancellationToken.None))
            .ReturnsAsync((Subscription?)null);

        // Act
        var result = await _subscriptionService.GetCurrentSubscriptionAsync(organizationId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetOrganizationSubscriptionsAsync Tests

    [Fact]
    public async Task GetOrganizationSubscriptionsAsync_WithSubscriptions_ShouldReturnAllSubscriptions()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var subscriptions = TestDataFactory.SubscriptionFaker.Generate(3);
        foreach (var sub in subscriptions)
        {
            sub.OrganizationId = organizationId;
        }

        var expectedResponses = subscriptions.Select(s => new SubscriptionResponse
        {
            Id = s.Id,
            OrganizationId = s.OrganizationId,
            Plan = s.Plan,
            Status = s.Status
        }).ToList();

        _mockSubscriptionRepository
            .Setup(x => x.GetByOrganizationAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(subscriptions);

        _mockMapper
            .Setup(x => x.Map<IEnumerable<SubscriptionResponse>>(subscriptions))
            .Returns(expectedResponses);

        // Act
        var result = await _subscriptionService.GetOrganizationSubscriptionsAsync(organizationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetOrganizationSubscriptionsAsync_WithNoSubscriptions_ShouldReturnEmptyList()
    {
        // Arrange
        var organizationId = Guid.NewGuid();

        _mockSubscriptionRepository
            .Setup(x => x.GetByOrganizationAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(new List<Subscription>());

        _mockMapper
            .Setup(x => x.Map<IEnumerable<SubscriptionResponse>>(It.IsAny<IEnumerable<Subscription>>()))
            .Returns(new List<SubscriptionResponse>());

        // Act
        var result = await _subscriptionService.GetOrganizationSubscriptionsAsync(organizationId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region CreateSubscriptionAsync Tests

    [Fact]
    public async Task CreateSubscriptionAsync_WithValidData_ShouldCreateSubscription()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var plan = SubscriptionPlan.Professional;
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.Id = organizationId;

        var expectedSubscription = new Subscription
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

        var expectedResponse = new SubscriptionResponse
        {
            Id = expectedSubscription.Id,
            OrganizationId = expectedSubscription.OrganizationId,
            Plan = expectedSubscription.Plan,
            Status = expectedSubscription.Status
        };

        _mockOrganizationRepository
            .Setup(x => x.GetByIdAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(organization);

        _mockSubscriptionRepository
            .Setup(x => x.GetActiveByOrganizationAsync(organizationId, CancellationToken.None))
            .ReturnsAsync((Subscription?)null);

        _mockSubscriptionRepository
            .Setup(x => x.AddAsync(It.IsAny<Subscription>(), CancellationToken.None))
            .ReturnsAsync((Subscription sub, CancellationToken token) => sub);

        _mockMapper
            .Setup(x => x.Map<SubscriptionResponse>(It.IsAny<Subscription>()))
            .Returns(expectedResponse);

        // Act
        var result = await _subscriptionService.CreateSubscriptionAsync(organizationId, plan);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedSubscription.Id, result.Id);
        Assert.Equal(expectedSubscription.OrganizationId, result.OrganizationId);
        Assert.Equal(expectedSubscription.Plan, result.Plan);
        Assert.Equal(expectedSubscription.Status, result.Status);

        _mockSubscriptionRepository.Verify(x => x.AddAsync(It.IsAny<Subscription>(), CancellationToken.None), Times.Once);
        _mockSubscriptionRepository.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CreateSubscriptionAsync_WithNonExistentOrganization_ShouldThrowException()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var plan = SubscriptionPlan.Basic;

        _mockOrganizationRepository
            .Setup(x => x.GetByIdAsync(organizationId, CancellationToken.None))
            .ReturnsAsync((Organization?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<OrganizationDomainException>(
            () => _subscriptionService.CreateSubscriptionAsync(organizationId, plan));

        Assert.Equal($"Organization with ID {organizationId} not found", exception.Message);
    }

    [Fact]
    public async Task CreateSubscriptionAsync_WithExistingActiveSubscription_ShouldThrowException()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var plan = SubscriptionPlan.Basic;
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.Id = organizationId;

        var existingSubscription = TestDataFactory.SubscriptionFaker.Generate();
        existingSubscription.OrganizationId = organizationId;
        existingSubscription.Status = SubscriptionStatus.Active;

        _mockOrganizationRepository
            .Setup(x => x.GetByIdAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(organization);

        _mockSubscriptionRepository
            .Setup(x => x.GetActiveByOrganizationAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(existingSubscription);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<OrganizationDomainException>(
            () => _subscriptionService.CreateSubscriptionAsync(organizationId, plan));

        Assert.Equal("Organization already has an active subscription", exception.Message);
    }

    #endregion

    #region ChangeSubscriptionPlanAsync Tests

    [Fact]
    public async Task ChangeSubscriptionPlanAsync_WithValidUpgrade_ShouldChangePlan()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var newPlan = SubscriptionPlan.Professional;
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Id = subscriptionId;
        subscription.Plan = SubscriptionPlan.Basic;
        subscription.Status = SubscriptionStatus.Active;

        var expectedResponse = new SubscriptionResponse
        {
            Id = subscription.Id,
            Plan = newPlan,
            Status = subscription.Status
        };

        _mockSubscriptionRepository
            .Setup(x => x.GetByIdAsync(subscriptionId, CancellationToken.None))
            .ReturnsAsync(subscription);

        _mockMapper
            .Setup(x => x.Map<SubscriptionResponse>(subscription))
            .Returns(expectedResponse);

        // Act
        var result = await _subscriptionService.ChangeSubscriptionPlanAsync(subscriptionId, newPlan);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newPlan, result.Plan);
        _mockSubscriptionRepository.Verify(x => x.UpdateAsync(subscription, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ChangeSubscriptionPlanAsync_WithNonExistentSubscription_ShouldThrowException()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var newPlan = SubscriptionPlan.Professional;

        _mockSubscriptionRepository
            .Setup(x => x.GetByIdAsync(subscriptionId, CancellationToken.None))
            .ReturnsAsync((Subscription?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SubscriptionDomainException>(
            () => _subscriptionService.ChangeSubscriptionPlanAsync(subscriptionId, newPlan));

        Assert.Equal($"Subscription with ID {subscriptionId} not found", exception.Message);
    }

    [Fact]
    public async Task ChangeSubscriptionPlanAsync_WithInactiveSubscription_ShouldThrowException()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var newPlan = SubscriptionPlan.Professional;
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Id = subscriptionId;
        subscription.Status = SubscriptionStatus.Cancelled;

        _mockSubscriptionRepository
            .Setup(x => x.GetByIdAsync(subscriptionId, CancellationToken.None))
            .ReturnsAsync(subscription);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SubscriptionDomainException>(
            () => _subscriptionService.ChangeSubscriptionPlanAsync(subscriptionId, newPlan));

        Assert.Equal("Cannot change plan for inactive subscription", exception.Message);
    }

    [Fact]
    public async Task ChangeSubscriptionPlanAsync_WithSamePlan_ShouldReturnExistingSubscription()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var plan = SubscriptionPlan.Professional;
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Id = subscriptionId;
        subscription.Plan = plan;
        subscription.Status = SubscriptionStatus.Active;

        var expectedResponse = new SubscriptionResponse
        {
            Id = subscription.Id,
            Plan = plan,
            Status = subscription.Status
        };

        _mockSubscriptionRepository
            .Setup(x => x.GetByIdAsync(subscriptionId, CancellationToken.None))
            .ReturnsAsync(subscription);

        _mockMapper
            .Setup(x => x.Map<SubscriptionResponse>(subscription))
            .Returns(expectedResponse);

        // Act
        var result = await _subscriptionService.ChangeSubscriptionPlanAsync(subscriptionId, plan);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(plan, result.Plan);
        _mockSubscriptionRepository.Verify(x => x.UpdateAsync(It.IsAny<Subscription>(), CancellationToken.None), Times.Never);
    }

    #endregion

    #region CancelSubscriptionAsync Tests

    [Fact]
    public async Task CancelSubscriptionAsync_WithActiveSubscription_ShouldCancelSubscription()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Id = subscriptionId;
        subscription.Status = SubscriptionStatus.Active;

        var expectedResponse = new SubscriptionResponse
        {
            Id = subscription.Id,
            Status = SubscriptionStatus.Cancelled
        };

        _mockSubscriptionRepository
            .Setup(x => x.GetByIdAsync(subscriptionId, CancellationToken.None))
            .ReturnsAsync(subscription);

        _mockMapper
            .Setup(x => x.Map<SubscriptionResponse>(subscription))
            .Returns(expectedResponse);

        // Act
        var result = await _subscriptionService.CancelSubscriptionAsync(subscriptionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SubscriptionStatus.Cancelled, result.Status);
        _mockSubscriptionRepository.Verify(x => x.UpdateAsync(subscription, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CancelSubscriptionAsync_WithNonExistentSubscription_ShouldThrowException()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();

        _mockSubscriptionRepository
            .Setup(x => x.GetByIdAsync(subscriptionId, CancellationToken.None))
            .ReturnsAsync((Subscription?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SubscriptionDomainException>(
            () => _subscriptionService.CancelSubscriptionAsync(subscriptionId));

        Assert.Equal($"Subscription with ID {subscriptionId} not found", exception.Message);
    }

    [Fact]
    public async Task CancelSubscriptionAsync_WithAlreadyCancelledSubscription_ShouldThrowException()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Id = subscriptionId;
        subscription.Status = SubscriptionStatus.Cancelled;

        _mockSubscriptionRepository
            .Setup(x => x.GetByIdAsync(subscriptionId, CancellationToken.None))
            .ReturnsAsync(subscription);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SubscriptionDomainException>(
            () => _subscriptionService.CancelSubscriptionAsync(subscriptionId));

        Assert.Equal("Subscription is already cancelled", exception.Message);
    }

    #endregion

    #region ReactivateSubscriptionAsync Tests

    [Fact]
    public async Task ReactivateSubscriptionAsync_WithCancelledSubscription_ShouldReactivateSubscription()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Id = subscriptionId;
        subscription.Status = SubscriptionStatus.Cancelled;

        var expectedResponse = new SubscriptionResponse
        {
            Id = subscription.Id,
            Status = SubscriptionStatus.Active
        };

        _mockSubscriptionRepository
            .Setup(x => x.GetByIdAsync(subscriptionId, CancellationToken.None))
            .ReturnsAsync(subscription);

        _mockMapper
            .Setup(x => x.Map<SubscriptionResponse>(subscription))
            .Returns(expectedResponse);

        // Act
        var result = await _subscriptionService.ReactivateSubscriptionAsync(subscriptionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SubscriptionStatus.Active, result.Status);
        _mockSubscriptionRepository.Verify(x => x.UpdateAsync(subscription, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ReactivateSubscriptionAsync_WithNonExistentSubscription_ShouldThrowException()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();

        _mockSubscriptionRepository
            .Setup(x => x.GetByIdAsync(subscriptionId, CancellationToken.None))
            .ReturnsAsync((Subscription?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SubscriptionDomainException>(
            () => _subscriptionService.ReactivateSubscriptionAsync(subscriptionId));

        Assert.Equal($"Subscription with ID {subscriptionId} not found", exception.Message);
    }

    [Fact]
    public async Task ReactivateSubscriptionAsync_WithActiveSubscription_ShouldThrowException()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Id = subscriptionId;
        subscription.Status = SubscriptionStatus.Active;

        _mockSubscriptionRepository
            .Setup(x => x.GetByIdAsync(subscriptionId, CancellationToken.None))
            .ReturnsAsync(subscription);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SubscriptionDomainException>(
            () => _subscriptionService.ReactivateSubscriptionAsync(subscriptionId));

        Assert.Equal("Subscription is already active", exception.Message);
    }

    #endregion

    #region TrackTranscriptionUsageAsync Tests

    [Fact]
    public async Task TrackTranscriptionUsageAsync_WithValidUsage_ShouldTrackUsage()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var minutesUsed = 30;
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Id = subscriptionId;
        subscription.Status = SubscriptionStatus.Active;
        subscription.Plan = SubscriptionPlan.Professional;
        subscription.TranscriptionMinutesUsed = 0;
        subscription.MaxTranscriptionMinutes = 600; // 10 hours

        var expectedResponse = new SubscriptionResponse
        {
            Id = subscription.Id,
            TranscriptionMinutesUsed = minutesUsed,
            RemainingTranscriptionMinutes = subscription.MaxTranscriptionMinutes - minutesUsed
        };

        _mockSubscriptionRepository
            .Setup(x => x.GetByIdAsync(subscriptionId, CancellationToken.None))
            .ReturnsAsync(subscription);

        _mockMapper
            .Setup(x => x.Map<SubscriptionResponse>(subscription))
            .Returns(expectedResponse);

        // Act
        var result = await _subscriptionService.TrackTranscriptionUsageAsync(subscriptionId, minutesUsed);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(minutesUsed, result.TranscriptionMinutesUsed);
        _mockSubscriptionRepository.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task TrackTranscriptionUsageAsync_WithNonExistentSubscription_ShouldThrowException()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var minutesUsed = 30;

        _mockSubscriptionRepository
            .Setup(x => x.GetByIdAsync(subscriptionId, CancellationToken.None))
            .ReturnsAsync((Subscription?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SubscriptionDomainException>(
            () => _subscriptionService.TrackTranscriptionUsageAsync(subscriptionId, minutesUsed));

        Assert.Equal($"Subscription with ID {subscriptionId} not found", exception.Message);
    }

    [Fact]
    public async Task TrackTranscriptionUsageAsync_WithInactiveSubscription_ShouldThrowException()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var minutesUsed = 30;
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Id = subscriptionId;
        subscription.Status = SubscriptionStatus.Cancelled;

        _mockSubscriptionRepository
            .Setup(x => x.GetByIdAsync(subscriptionId, CancellationToken.None))
            .ReturnsAsync(subscription);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SubscriptionDomainException>(
            () => _subscriptionService.TrackTranscriptionUsageAsync(subscriptionId, minutesUsed));

        Assert.Equal("Cannot track usage for inactive subscription", exception.Message);
    }

    [Fact]
    public async Task TrackTranscriptionUsageAsync_WithInsufficientMinutes_ShouldThrowException()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var minutesUsed = 700; // More than available
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Id = subscriptionId;
        subscription.Status = SubscriptionStatus.Active;
        subscription.Plan = SubscriptionPlan.Professional;
        subscription.TranscriptionMinutesUsed = 0;
        subscription.MaxTranscriptionMinutes = 600; // 10 hours

        _mockSubscriptionRepository
            .Setup(x => x.GetByIdAsync(subscriptionId, CancellationToken.None))
            .ReturnsAsync(subscription);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SubscriptionDomainException>(
            () => _subscriptionService.TrackTranscriptionUsageAsync(subscriptionId, minutesUsed));

        Assert.Equal($"Insufficient transcription minutes remaining. Requested: {minutesUsed}, Available: {subscription.MaxTranscriptionMinutes}", exception.Message);
    }

    #endregion

    #region ResetMonthlyUsageAsync Tests

    [Fact]
    public async Task ResetMonthlyUsageAsync_WithValidSubscription_ShouldResetUsage()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Id = subscriptionId;
        subscription.TranscriptionMinutesUsed = 300;

        var expectedResponse = new SubscriptionResponse
        {
            Id = subscription.Id,
            TranscriptionMinutesUsed = 0
        };

        _mockSubscriptionRepository
            .Setup(x => x.GetByIdAsync(subscriptionId, CancellationToken.None))
            .ReturnsAsync(subscription);

        _mockMapper
            .Setup(x => x.Map<SubscriptionResponse>(subscription))
            .Returns(expectedResponse);

        // Act
        var result = await _subscriptionService.ResetMonthlyUsageAsync(subscriptionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TranscriptionMinutesUsed);
        _mockSubscriptionRepository.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ResetMonthlyUsageAsync_WithNonExistentSubscription_ShouldThrowException()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();

        _mockSubscriptionRepository
            .Setup(x => x.GetByIdAsync(subscriptionId, CancellationToken.None))
            .ReturnsAsync((Subscription?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SubscriptionDomainException>(
            () => _subscriptionService.ResetMonthlyUsageAsync(subscriptionId));

        Assert.Equal($"Subscription with ID {subscriptionId} not found", exception.Message);
    }

    #endregion

    #region CanUseTranscriptionMinutesAsync Tests

    [Fact]
    public async Task CanUseTranscriptionMinutesAsync_WithSufficientMinutes_ShouldReturnTrue()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var minutes = 30;
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.OrganizationId = organizationId;
        subscription.Status = SubscriptionStatus.Active;
        subscription.Plan = SubscriptionPlan.Professional;
        subscription.TranscriptionMinutesUsed = 0;
        subscription.MaxTranscriptionMinutes = 600;

        _mockSubscriptionRepository
            .Setup(x => x.GetActiveByOrganizationAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(subscription);

        // Act
        var result = await _subscriptionService.CanUseTranscriptionMinutesAsync(organizationId, minutes);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanUseTranscriptionMinutesAsync_WithInsufficientMinutes_ShouldReturnFalse()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var minutes = 700;
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.OrganizationId = organizationId;
        subscription.Status = SubscriptionStatus.Active;
        subscription.Plan = SubscriptionPlan.Professional;
        subscription.TranscriptionMinutesUsed = 0;
        subscription.MaxTranscriptionMinutes = 600;

        _mockSubscriptionRepository
            .Setup(x => x.GetActiveByOrganizationAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(subscription);

        // Act
        var result = await _subscriptionService.CanUseTranscriptionMinutesAsync(organizationId, minutes);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanUseTranscriptionMinutesAsync_WithNoActiveSubscription_ShouldReturnFalse()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var minutes = 30;

        _mockSubscriptionRepository
            .Setup(x => x.GetActiveByOrganizationAsync(organizationId, CancellationToken.None))
            .ReturnsAsync((Subscription?)null);

        // Act
        var result = await _subscriptionService.CanUseTranscriptionMinutesAsync(organizationId, minutes);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetUsageAnalyticsAsync Tests

    [Fact]
    public async Task GetUsageAnalyticsAsync_WithActiveSubscription_ShouldReturnUsageAnalytics()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.OrganizationId = organizationId;
        subscription.Status = SubscriptionStatus.Active;
        subscription.Plan = SubscriptionPlan.Professional;
        subscription.TranscriptionMinutesUsed = 300;
        subscription.MaxTranscriptionMinutes = 600;
        subscription.UsageResetDate = DateTime.UtcNow.AddDays(15);

        var totalUsage = 1200;

        _mockSubscriptionRepository
            .Setup(x => x.GetActiveByOrganizationAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(subscription);

        _mockSubscriptionRepository
            .Setup(x => x.GetTotalUsageMinutesAsync(organizationId, null, CancellationToken.None))
            .ReturnsAsync(totalUsage);

        // Act
        var result = await _subscriptionService.GetUsageAnalyticsAsync(organizationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(organizationId, result.OrganizationId);
        Assert.Equal(subscription.Plan, result.CurrentPlan);
        Assert.Equal(subscription.Plan.ToString(), result.PlanName);
        Assert.Equal(subscription.MaxTranscriptionMinutes, result.MonthlyLimit);
        Assert.Equal(subscription.TranscriptionMinutesUsed, result.MinutesUsed);
        Assert.Equal(subscription.RemainingTranscriptionMinutes, result.MinutesRemaining);
        Assert.Equal(totalUsage, result.TotalUsage);
        Assert.Equal(50, result.UsagePercentage); // 300/600 * 100
        Assert.Equal(subscription.UsageResetDate, result.UsageResetDate);
        Assert.False(result.IsNearLimit); // 300 remaining > 120
    }

    [Fact]
    public async Task GetUsageAnalyticsAsync_WithNoActiveSubscription_ShouldThrowException()
    {
        // Arrange
        var organizationId = Guid.NewGuid();

        _mockSubscriptionRepository
            .Setup(x => x.GetActiveByOrganizationAsync(organizationId, CancellationToken.None))
            .ReturnsAsync((Subscription?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SubscriptionDomainException>(
            () => _subscriptionService.GetUsageAnalyticsAsync(organizationId));

        Assert.Equal($"No active subscription found for organization {organizationId}", exception.Message);
    }

    [Fact]
    public async Task GetUsageAnalyticsAsync_WithNearLimit_ShouldReturnIsNearLimitTrue()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.OrganizationId = organizationId;
        subscription.Status = SubscriptionStatus.Active;
        subscription.Plan = SubscriptionPlan.Professional;
        subscription.TranscriptionMinutesUsed = 580; // 20 minutes remaining
        subscription.MaxTranscriptionMinutes = 600;

        _mockSubscriptionRepository
            .Setup(x => x.GetActiveByOrganizationAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(subscription);

        _mockSubscriptionRepository
            .Setup(x => x.GetTotalUsageMinutesAsync(organizationId, null, CancellationToken.None))
            .ReturnsAsync(1000);

        // Act
        var result = await _subscriptionService.GetUsageAnalyticsAsync(organizationId);

        // Assert
        Assert.True(result.IsNearLimit); // 20 remaining <= 120
    }

    #endregion

    #region GetAvailablePlansAsync Tests

    [Fact]
    public async Task GetAvailablePlansAsync_ShouldReturnAllPlans()
    {
        // Act
        var result = await _subscriptionService.GetAvailablePlansAsync();

        // Assert
        Assert.NotNull(result);
        var plans = result.ToList();
        Assert.Equal(3, plans.Count); // Basic, Professional, Enterprise

        var basicPlan = plans.FirstOrDefault(p => p.Plan == SubscriptionPlan.Basic);
        Assert.NotNull(basicPlan);
        Assert.Equal("Basic", basicPlan.PlanName);
        Assert.Equal(360, basicPlan.MaxTranscriptionMinutes); // 6 hours
        Assert.True(basicPlan.CanExportTranscriptions);
        Assert.True(basicPlan.HasRealtimeTranscription);
        Assert.False(basicPlan.HasPrioritySupport);

        var professionalPlan = plans.FirstOrDefault(p => p.Plan == SubscriptionPlan.Professional);
        Assert.NotNull(professionalPlan);
        Assert.Equal("Professional", professionalPlan.PlanName);
        Assert.Equal(600, professionalPlan.MaxTranscriptionMinutes); // 10 hours
        Assert.True(professionalPlan.HasPrioritySupport);

        var enterprisePlan = plans.FirstOrDefault(p => p.Plan == SubscriptionPlan.Enterprise);
        Assert.NotNull(enterprisePlan);
        Assert.Equal("Enterprise", enterprisePlan.PlanName);
        Assert.Equal(840, enterprisePlan.MaxTranscriptionMinutes); // 14 hours
        Assert.True(enterprisePlan.HasPrioritySupport);
    }

    [Fact]
    public async Task GetAvailablePlansAsync_ShouldIncludeAllFeatures()
    {
        // Act
        var result = await _subscriptionService.GetAvailablePlansAsync();

        // Assert
        var plans = result.ToList();

        var basicPlan = plans.FirstOrDefault(p => p.Plan == SubscriptionPlan.Basic);
        Assert.NotNull(basicPlan);
        Assert.Contains("Unlimited users", basicPlan.Features);
        Assert.Contains("6 hours of transcription per month", basicPlan.Features);
        Assert.Contains("Real-time transcription", basicPlan.Features);
        Assert.Contains("Export transcriptions", basicPlan.Features);
        Assert.Contains("Email support", basicPlan.Features);

        var professionalPlan = plans.FirstOrDefault(p => p.Plan == SubscriptionPlan.Professional);
        Assert.NotNull(professionalPlan);
        Assert.Contains("Priority support", professionalPlan.Features);
        Assert.Contains("Advanced analytics", professionalPlan.Features);

        var enterprisePlan = plans.FirstOrDefault(p => p.Plan == SubscriptionPlan.Enterprise);
        Assert.NotNull(enterprisePlan);
        Assert.Contains("Custom integrations", enterprisePlan.Features);
        Assert.Contains("Dedicated account manager", enterprisePlan.Features);
    }

    #endregion
}
