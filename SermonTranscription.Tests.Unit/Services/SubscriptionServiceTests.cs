using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using SermonTranscription.Application.Common;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Services;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
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
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(subscription.Id, result.Data.Id);
        Assert.Equal(subscription.OrganizationId, result.Data.OrganizationId);
        Assert.Equal(subscription.Plan, result.Data.Plan);
        Assert.Equal(subscription.Status, result.Data.Status);
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
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Data);
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
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data.Count());
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
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data);
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
        Assert.Equal(expectedSubscription.Id, result.Data.Id);
        Assert.Equal(expectedSubscription.OrganizationId, result.Data.OrganizationId);
        Assert.Equal(expectedSubscription.Plan, result.Data.Plan);
        Assert.Equal(expectedSubscription.Status, result.Data.Status);

        _mockSubscriptionRepository.Verify(x => x.AddAsync(It.IsAny<Subscription>(), CancellationToken.None), Times.Once);
        _mockSubscriptionRepository.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CreateSubscriptionAsync_WithNonExistentOrganization_ShouldErrorNotFound()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var plan = SubscriptionPlan.Basic;

        _mockOrganizationRepository
            .Setup(x => x.GetByIdAsync(organizationId, CancellationToken.None))
            .ReturnsAsync((Organization?)null);

        // Act & Assert
        var result = await _subscriptionService.CreateSubscriptionAsync(organizationId, plan);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.ErrorCode == ErrorCode.NotFound);
    }

    [Fact]
    public async Task CreateSubscriptionAsync_WithExistingActiveSubscription_ShouldErrorConflict()
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
        var result = await _subscriptionService.CreateSubscriptionAsync(organizationId, plan);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.ErrorCode == ErrorCode.Conflict);
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
        Assert.Equal(newPlan, result.Data.Plan);
        _mockSubscriptionRepository.Verify(x => x.UpdateAsync(subscription, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ChangeSubscriptionPlanAsync_WithNonExistentSubscription_ShouldErrorNotFound()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var newPlan = SubscriptionPlan.Professional;

        _mockSubscriptionRepository
            .Setup(x => x.GetByIdAsync(subscriptionId, CancellationToken.None))
            .ReturnsAsync((Subscription?)null);

        // Act & Assert
        var result = await _subscriptionService.ChangeSubscriptionPlanAsync(subscriptionId, newPlan);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.ErrorCode == ErrorCode.NotFound);
    }

    [Fact]
    public async Task ChangeSubscriptionPlanAsync_WithInactiveSubscription_ShouldErrorForbidden()
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
        var result = await _subscriptionService.ChangeSubscriptionPlanAsync(subscriptionId, newPlan);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.ErrorCode == ErrorCode.Forbidden);
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
        Assert.Equal(plan, result.Data.Plan);
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
        Assert.Equal(SubscriptionStatus.Cancelled, result.Data.Status);
        _mockSubscriptionRepository.Verify(x => x.UpdateAsync(subscription, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CancelSubscriptionAsync_WithNonExistentSubscription_ShouldErrorNotFound()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();

        _mockSubscriptionRepository
            .Setup(x => x.GetByIdAsync(subscriptionId, CancellationToken.None))
            .ReturnsAsync((Subscription?)null);

        // Act & Assert
        var result = await _subscriptionService.CancelSubscriptionAsync(subscriptionId);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.ErrorCode == ErrorCode.NotFound);
    }

    [Fact]
    public async Task CancelSubscriptionAsync_WithAlreadyCancelledSubscription_ShouldErrorConflict()
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
        var result = await _subscriptionService.CancelSubscriptionAsync(subscriptionId);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.ErrorCode == ErrorCode.Conflict);
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
        Assert.Equal(SubscriptionStatus.Active, result.Data.Status);
        _mockSubscriptionRepository.Verify(x => x.UpdateAsync(subscription, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ReactivateSubscriptionAsync_WithNonExistentSubscription_ShouldErrorNotFound()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();

        _mockSubscriptionRepository
            .Setup(x => x.GetByIdAsync(subscriptionId, CancellationToken.None))
            .ReturnsAsync((Subscription?)null);

        // Act & Assert
        var result = await _subscriptionService.ReactivateSubscriptionAsync(subscriptionId);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.ErrorCode == ErrorCode.NotFound);
    }

    [Fact]
    public async Task ReactivateSubscriptionAsync_WithActiveSubscription_ShouldErrorConflict()
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
        var result = await _subscriptionService.ReactivateSubscriptionAsync(subscriptionId);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.ErrorCode == ErrorCode.Conflict);
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
        Assert.Equal(minutesUsed, result.Data.TranscriptionMinutesUsed);
        _mockSubscriptionRepository.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task TrackTranscriptionUsageAsync_WithNonExistentSubscription_ShouldErrorNotFound()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var minutesUsed = 30;

        _mockSubscriptionRepository
            .Setup(x => x.GetByIdAsync(subscriptionId, CancellationToken.None))
            .ReturnsAsync((Subscription?)null);

        // Act & Assert
        var result = await _subscriptionService.TrackTranscriptionUsageAsync(subscriptionId, minutesUsed);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.ErrorCode == ErrorCode.NotFound);
    }

    [Fact]
    public async Task TrackTranscriptionUsageAsync_WithInactiveSubscription_ShouldErrorForbidden()
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
        var result = await _subscriptionService.TrackTranscriptionUsageAsync(subscriptionId, minutesUsed);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.ErrorCode == ErrorCode.Forbidden);
    }

    [Fact]
    public async Task TrackTranscriptionUsageAsync_WithInsufficientMinutes_ShouldErrorValidationError()
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
        var result = await _subscriptionService.TrackTranscriptionUsageAsync(subscriptionId, minutesUsed);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.ErrorCode == ErrorCode.ValidationError);
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
        Assert.Equal(0, result.Data.TranscriptionMinutesUsed);
        _mockSubscriptionRepository.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ResetMonthlyUsageAsync_WithNonExistentSubscription_ShouldErrorNotFound()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();

        _mockSubscriptionRepository
            .Setup(x => x.GetByIdAsync(subscriptionId, CancellationToken.None))
            .ReturnsAsync((Subscription?)null);

        // Act & Assert
        var result = await _subscriptionService.ResetMonthlyUsageAsync(subscriptionId);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.ErrorCode == ErrorCode.NotFound);
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
        Assert.True(result.IsSuccess);
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
        Assert.True(result.IsSuccess);
        Assert.False(result.Data);
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
        Assert.False(result.IsSuccess);
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

        _mockSubscriptionRepository
            .Setup(x => x.GetActiveByOrganizationAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(subscription);

        // Act
        var result = await _subscriptionService.GetUsageAnalyticsAsync(organizationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(organizationId, result.Data.OrganizationId);
        Assert.Equal(subscription.Plan, result.Data.CurrentPlan);
        Assert.Equal(subscription.Plan.ToString(), result.Data.PlanName);
        Assert.Equal(subscription.MaxTranscriptionMinutes, result.Data.MonthlyLimit);
        Assert.Equal(subscription.TranscriptionMinutesUsed, result.Data.MinutesUsed);
        Assert.Equal(subscription.RemainingTranscriptionMinutes, result.Data.MinutesRemaining);
        Assert.Equal(subscription.TranscriptionMinutesUsed, result.Data.TotalUsage);
        Assert.Equal(50, result.Data.UsagePercentage); // 300/600 * 100
        Assert.Equal(subscription.UsageResetDate, result.Data.UsageResetDate);
        Assert.False(result.Data.IsNearLimit); // 300 remaining > 120
    }

    [Fact]
    public async Task GetUsageAnalyticsAsync_WithNoActiveSubscription_ShouldErrorNotFound()
    {
        // Arrange
        var organizationId = Guid.NewGuid();

        _mockSubscriptionRepository
            .Setup(x => x.GetActiveByOrganizationAsync(organizationId, CancellationToken.None))
            .ReturnsAsync((Subscription?)null);

        // Act & Assert
        var result = await _subscriptionService.GetUsageAnalyticsAsync(organizationId);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.ErrorCode == ErrorCode.NotFound);
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

        // Act
        var result = await _subscriptionService.GetUsageAnalyticsAsync(organizationId);

        // Assert
        Assert.True(result.Data.IsNearLimit); // 20 remaining <= 120
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
        var plans = result.Data.ToList();
        Assert.Equal(3, plans.Count); // Basic, Professional, Enterprise

        var basicPlan = plans.FirstOrDefault(p => p.Plan == SubscriptionPlan.Basic);
        Assert.NotNull(basicPlan);
        Assert.Equal("Basic", basicPlan.PlanName);
        Assert.Equal(300, basicPlan.MaxTranscriptionMinutes); // 5 hours
        Assert.True(basicPlan.CanExportTranscriptions);
        Assert.False(basicPlan.HasRealtimeTranscription);
        Assert.False(basicPlan.HasPrioritySupport);

        var professionalPlan = plans.FirstOrDefault(p => p.Plan == SubscriptionPlan.Professional);
        Assert.NotNull(professionalPlan);
        Assert.Equal("Professional", professionalPlan.PlanName);
        Assert.Equal(1000, professionalPlan.MaxTranscriptionMinutes); // 1000 minutes
        Assert.True(professionalPlan.HasPrioritySupport);

        var enterprisePlan = plans.FirstOrDefault(p => p.Plan == SubscriptionPlan.Enterprise);
        Assert.NotNull(enterprisePlan);
        Assert.Equal("Enterprise", enterprisePlan.PlanName);
        Assert.Equal(5000, enterprisePlan.MaxTranscriptionMinutes); // 5000 minutes
        Assert.True(enterprisePlan.HasPrioritySupport);
    }

    [Fact]
    public async Task GetAvailablePlansAsync_ShouldIncludeAllFeatures()
    {
        // Act
        var result = await _subscriptionService.GetAvailablePlansAsync();

        // Assert
        var plans = result.Data.ToList();

        var basicPlan = plans.FirstOrDefault(p => p.Plan == SubscriptionPlan.Basic);
        Assert.NotNull(basicPlan);
        Assert.Contains("300 minutes of transcription per month", basicPlan.Features);
        Assert.Contains("Improved transcription accuracy", basicPlan.Features);
        Assert.Contains("Email support", basicPlan.Features);
        Assert.Contains("Export to common formats", basicPlan.Features);

        var professionalPlan = plans.FirstOrDefault(p => p.Plan == SubscriptionPlan.Professional);
        Assert.NotNull(professionalPlan);
        Assert.Contains("Priority support", professionalPlan.Features);
        Assert.Contains("Analytics dashboard", professionalPlan.Features);

        var enterprisePlan = plans.FirstOrDefault(p => p.Plan == SubscriptionPlan.Enterprise);
        Assert.NotNull(enterprisePlan);
        Assert.Contains("Custom integrations", enterprisePlan.Features);
        Assert.Contains("Dedicated support", enterprisePlan.Features);
    }

    #endregion
}
