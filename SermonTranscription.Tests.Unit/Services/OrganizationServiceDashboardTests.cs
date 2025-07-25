using Microsoft.Extensions.Logging;
using Moq;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Services;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Interfaces;
using SermonTranscription.Tests.Unit.Common;
using Xunit;

namespace SermonTranscription.Tests.Unit.Services;

public class OrganizationServiceDashboardTests : BaseUnitTest
{
    private readonly Mock<IOrganizationRepository> _mockOrganizationRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IUserOrganizationRepository> _mockUserOrganizationRepository;
    private readonly Mock<ITranscriptionSessionRepository> _mockTranscriptionSessionRepository;
    private readonly Mock<ITranscriptionRepository> _mockTranscriptionRepository;
    private readonly Mock<ISubscriptionRepository> _mockSubscriptionRepository;
    private readonly Mock<ILogger<OrganizationService>> _mockLogger;
    private readonly OrganizationService _organizationService;

    public OrganizationServiceDashboardTests()
    {
        _mockOrganizationRepository = new Mock<IOrganizationRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockUserOrganizationRepository = new Mock<IUserOrganizationRepository>();
        _mockTranscriptionSessionRepository = new Mock<ITranscriptionSessionRepository>();
        _mockTranscriptionRepository = new Mock<ITranscriptionRepository>();
        _mockSubscriptionRepository = new Mock<ISubscriptionRepository>();
        _mockLogger = new Mock<ILogger<OrganizationService>>();

        _organizationService = new OrganizationService(
            _mockOrganizationRepository.Object,
            _mockUserRepository.Object,
            _mockUserOrganizationRepository.Object,
            _mockTranscriptionSessionRepository.Object,
            _mockTranscriptionRepository.Object,
            _mockSubscriptionRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetOrganizationDashboardAsync_WithValidOrganization_ShouldReturnDashboardData()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.Id = organizationId;

        var users = TestDataFactory.UserFaker.Generate(3);
        var userOrganizations = users.Select((user, index) => new UserOrganization
        {
            UserId = user.Id,
            OrganizationId = organizationId,
            User = user,
            Role = index == 0 ? UserRole.OrganizationAdmin : UserRole.OrganizationUser,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-index)
        }).ToList();

        var sessions = TestDataFactory.TranscriptionSessionFaker.Generate(5);
        foreach (var session in sessions)
        {
            session.OrganizationId = organizationId;
            session.CreatedByUserId = users[0].Id;
            session.CreatedByUser = users[0];
        }

        var transcriptions = TestDataFactory.TranscriptionFaker.Generate(10);
        foreach (var transcription in transcriptions)
        {
            transcription.OrganizationId = organizationId;
            transcription.CreatedByUserId = users[0].Id;
            transcription.CreatedByUser = users[0];
            transcription.DurationSeconds = 1800; // 30 minutes
        }

        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.OrganizationId = organizationId;
        subscription.Plan = SubscriptionPlan.Professional;
        subscription.Status = SubscriptionStatus.Active;
        subscription.TranscriptionMinutesUsed = 300;
        subscription.MaxTranscriptionMinutes = 600;

        _mockOrganizationRepository
            .Setup(x => x.GetByIdAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(organization);

        _mockUserOrganizationRepository
            .Setup(x => x.GetOrganizationUsersAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(userOrganizations);

        _mockUserOrganizationRepository
            .Setup(x => x.GetOrganizationPendingInvitationsAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(new List<UserOrganization>());

        _mockSubscriptionRepository
            .Setup(x => x.GetActiveByOrganizationAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(subscription);

        _mockSubscriptionRepository
            .Setup(x => x.GetTotalUsageMinutesAsync(organizationId, null, CancellationToken.None))
            .ReturnsAsync(1500);

        _mockTranscriptionSessionRepository
            .Setup(x => x.GetByOrganizationAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(sessions);

        _mockTranscriptionSessionRepository
            .Setup(x => x.GetActiveSessionsAsync(CancellationToken.None))
            .ReturnsAsync(sessions.Where(s => s.IsActive));

        _mockTranscriptionSessionRepository
            .Setup(x => x.GetRecentSessionsAsync(organizationId, 5, CancellationToken.None))
            .ReturnsAsync(sessions.Take(5));

        _mockTranscriptionRepository
            .Setup(x => x.GetByOrganizationAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(transcriptions);

        _mockTranscriptionRepository
            .Setup(x => x.GetRecentTranscriptionsAsync(organizationId, 5, CancellationToken.None))
            .ReturnsAsync(transcriptions.Take(5));

        _mockTranscriptionRepository
            .Setup(x => x.GetTotalDurationAsync(organizationId, null, CancellationToken.None))
            .ReturnsAsync(TimeSpan.FromMinutes(300));

        // Act
        var result = await _organizationService.GetOrganizationDashboardAsync(organizationId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        var dashboard = result.Data;

        // Overview assertions
        Assert.Equal(organizationId, dashboard.Overview.OrganizationId);
        Assert.Equal(organization.Name, dashboard.Overview.Name);
        Assert.Equal(3, dashboard.Overview.TotalUsers);
        Assert.Equal(5, dashboard.Overview.TotalSessions);
        Assert.Equal(10, dashboard.Overview.TotalTranscriptions);

        // User activity assertions
        Assert.Equal(3, dashboard.UserActivity.TotalUsers);
        Assert.Equal(1, dashboard.UserActivity.AdminUsers);
        Assert.Equal(2, dashboard.UserActivity.RegularUsers);
        Assert.Equal(0, dashboard.UserActivity.PendingInvitations);

        // Subscription status assertions
        Assert.Equal("Professional", dashboard.SubscriptionStatus.CurrentPlan);
        Assert.Equal("Active", dashboard.SubscriptionStatus.Status);
        Assert.Equal(600, dashboard.SubscriptionStatus.MonthlyLimit);
        Assert.Equal(300, dashboard.SubscriptionStatus.MinutesUsed);
        Assert.Equal(300, dashboard.SubscriptionStatus.MinutesRemaining);
        Assert.Equal(50, dashboard.SubscriptionStatus.UsagePercentage);
        Assert.False(dashboard.SubscriptionStatus.IsNearLimit);

        // Transcription stats assertions
        Assert.Equal(5, dashboard.TranscriptionStats.TotalSessions);
        Assert.Equal(10, dashboard.TranscriptionStats.TotalTranscriptions);
        Assert.Equal(300, dashboard.TranscriptionStats.TotalTranscriptionMinutes);
        Assert.Equal(60, dashboard.TranscriptionStats.AverageSessionDuration);

        // Recent activity assertions
        Assert.Equal(5, dashboard.RecentActivity.RecentSessions.Count);
        Assert.Equal(5, dashboard.RecentActivity.RecentTranscriptions.Count);
        Assert.Equal(3, dashboard.RecentActivity.RecentUserActivities.Count);
    }

    [Fact]
    public async Task GetOrganizationDashboardAsync_WithNonExistentOrganization_ShouldReturnFailure()
    {
        // Arrange
        var organizationId = Guid.NewGuid();

        _mockOrganizationRepository
            .Setup(x => x.GetByIdAsync(organizationId, CancellationToken.None))
            .ReturnsAsync((Organization?)null);

        // Act
        var result = await _organizationService.GetOrganizationDashboardAsync(organizationId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal($"Organization with ID {organizationId} not found", result.Message);
    }

    [Fact]
    public async Task GetOrganizationDashboardAsync_WithNoActiveSubscription_ShouldHandleGracefully()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.Id = organizationId;

        var users = TestDataFactory.UserFaker.Generate(2);
        var userOrganizations = users.Select(user => new UserOrganization
        {
            UserId = user.Id,
            OrganizationId = organizationId,
            User = user,
            Role = UserRole.OrganizationUser,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        _mockOrganizationRepository
            .Setup(x => x.GetByIdAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(organization);

        _mockUserOrganizationRepository
            .Setup(x => x.GetOrganizationUsersAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(userOrganizations);

        _mockUserOrganizationRepository
            .Setup(x => x.GetOrganizationPendingInvitationsAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(new List<UserOrganization>());

        _mockSubscriptionRepository
            .Setup(x => x.GetActiveByOrganizationAsync(organizationId, CancellationToken.None))
            .ReturnsAsync((Subscription?)null);

        _mockSubscriptionRepository
            .Setup(x => x.GetTotalUsageMinutesAsync(organizationId, null, CancellationToken.None))
            .ReturnsAsync(0);

        _mockTranscriptionSessionRepository
            .Setup(x => x.GetByOrganizationAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(new List<TranscriptionSession>());

        _mockTranscriptionSessionRepository
            .Setup(x => x.GetActiveSessionsAsync(CancellationToken.None))
            .ReturnsAsync(new List<TranscriptionSession>());

        _mockTranscriptionSessionRepository
            .Setup(x => x.GetRecentSessionsAsync(organizationId, 5, CancellationToken.None))
            .ReturnsAsync(new List<TranscriptionSession>());

        _mockTranscriptionRepository
            .Setup(x => x.GetByOrganizationAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(new List<Transcription>());

        _mockTranscriptionRepository
            .Setup(x => x.GetRecentTranscriptionsAsync(organizationId, 5, CancellationToken.None))
            .ReturnsAsync(new List<Transcription>());

        _mockTranscriptionRepository
            .Setup(x => x.GetTotalDurationAsync(organizationId, null, CancellationToken.None))
            .ReturnsAsync(TimeSpan.Zero);

        // Act
        var result = await _organizationService.GetOrganizationDashboardAsync(organizationId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        var dashboard = result.Data;
        Assert.Equal("No Plan", dashboard.SubscriptionStatus.CurrentPlan);
        Assert.Equal("Inactive", dashboard.SubscriptionStatus.Status);
        Assert.Equal(0, dashboard.SubscriptionStatus.MonthlyLimit);
        Assert.Equal(0, dashboard.SubscriptionStatus.MinutesUsed);
        Assert.Equal(0, dashboard.SubscriptionStatus.MinutesRemaining);
        Assert.Equal(0, dashboard.SubscriptionStatus.UsagePercentage);
    }

    [Fact]
    public async Task GetOrganizationDashboardAsync_WithNearLimitSubscription_ShouldSetIsNearLimitTrue()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.Id = organizationId;

        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.OrganizationId = organizationId;
        subscription.Plan = SubscriptionPlan.Basic;
        subscription.Status = SubscriptionStatus.Active;
        subscription.TranscriptionMinutesUsed = 340; // 20 minutes remaining (360 - 340 = 20)
        subscription.MaxTranscriptionMinutes = 360;

        _mockOrganizationRepository
            .Setup(x => x.GetByIdAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(organization);

        _mockUserOrganizationRepository
            .Setup(x => x.GetOrganizationUsersAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(new List<UserOrganization>());

        _mockUserOrganizationRepository
            .Setup(x => x.GetOrganizationPendingInvitationsAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(new List<UserOrganization>());

        _mockSubscriptionRepository
            .Setup(x => x.GetActiveByOrganizationAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(subscription);

        _mockSubscriptionRepository
            .Setup(x => x.GetTotalUsageMinutesAsync(organizationId, null, CancellationToken.None))
            .ReturnsAsync(340);

        _mockTranscriptionSessionRepository
            .Setup(x => x.GetByOrganizationAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(new List<TranscriptionSession>());

        _mockTranscriptionSessionRepository
            .Setup(x => x.GetActiveSessionsAsync(CancellationToken.None))
            .ReturnsAsync(new List<TranscriptionSession>());

        _mockTranscriptionSessionRepository
            .Setup(x => x.GetRecentSessionsAsync(organizationId, 5, CancellationToken.None))
            .ReturnsAsync(new List<TranscriptionSession>());

        _mockTranscriptionRepository
            .Setup(x => x.GetByOrganizationAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(new List<Transcription>());

        _mockTranscriptionRepository
            .Setup(x => x.GetRecentTranscriptionsAsync(organizationId, 5, CancellationToken.None))
            .ReturnsAsync(new List<Transcription>());

        _mockTranscriptionRepository
            .Setup(x => x.GetTotalDurationAsync(organizationId, null, CancellationToken.None))
            .ReturnsAsync(TimeSpan.Zero);

        // Act
        var result = await _organizationService.GetOrganizationDashboardAsync(organizationId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        var dashboard = result.Data;
        Assert.True(dashboard.SubscriptionStatus.IsNearLimit); // 20 minutes remaining <= 120
        Assert.Equal(20, dashboard.SubscriptionStatus.MinutesRemaining);
    }
}
