using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SermonTranscription.Tests.Integration.Common;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Domain.Enums;
using System.Net;
using System.Text.Json;

namespace SermonTranscription.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for subscription management endpoints
/// </summary>
public class SubscriptionsControllerTests : BaseIntegrationTest
{
    public SubscriptionsControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    #region GetAvailablePlans Tests

    [Fact]
    public async Task GetAvailablePlans_ShouldReturnAllSubscriptionPlans()
    {
        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/subscriptions/plans");

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.OK);

        var plans = await ReadJsonResponseAsync<List<SubscriptionPlanResponse>>(response);
        plans.Should().NotBeNull();
        plans!.Count.Should().BeGreaterThan(0);

        // Verify each plan has required properties
        foreach (var plan in plans)
        {
            plan.Plan.Should().BeDefined();
            plan.PlanName.Should().NotBeNullOrEmpty();
            plan.MonthlyPrice.Should().BeGreaterThan(0);
            plan.YearlyPrice.Should().BeGreaterThan(0);
            plan.MaxTranscriptionMinutes.Should().BeGreaterThan(0);
            plan.Features.Should().NotBeNull();
        }

        // Verify all subscription plan types are included
        var planTypes = plans.Select(p => p.Plan).ToList();
        planTypes.Should().Contain(SubscriptionPlan.Basic);
        planTypes.Should().Contain(SubscriptionPlan.Professional);
        planTypes.Should().Contain(SubscriptionPlan.Enterprise);
    }

    [Fact]
    public async Task GetAvailablePlans_ShouldNotRequireAuthentication()
    {
        // Arrange - No authentication headers set

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/subscriptions/plans");

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.OK);
    }

    #endregion

    #region GetCurrentSubscription Tests

    [Fact]
    public async Task GetCurrentSubscription_WithValidToken_ShouldReturnCurrentSubscription()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedAdminAsync();

        // Create a subscription for the organization
        var subscription = new Domain.Entities.Subscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Plan = SubscriptionPlan.Basic,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            MonthlyPrice = 29.99m,
            Currency = "USD",
            MaxTranscriptionMinutes = 1000,
            CanExportTranscriptions = true,
            HasRealtimeTranscription = true,
            HasPrioritySupport = false
        };

        DbContext.Subscriptions.Add(subscription);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/subscriptions/current");

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.OK);

        var result = await ReadJsonResponseAsync<SubscriptionResponse>(response);
        result.Should().NotBeNull();
        result!.Id.Should().Be(subscription.Id);
        result.Plan.Should().Be(SubscriptionPlan.Basic);
        result.Status.Should().Be(SubscriptionStatus.Active);
        result.OrganizationId.Should().Be(organization.Id);
        result.MonthlyPrice.Should().Be(29.99m);
        result.MaxTranscriptionMinutes.Should().Be(1000);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetCurrentSubscription_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange - No authentication headers set

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/subscriptions/current");

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentSubscription_WithoutOrganizationHeader_ShouldReturnForbidden()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var token = GenerateJwtTokenAsync(user);
        SetAuthorizationHeader(token);
        // Don't set organization header

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/subscriptions/current");

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetCurrentSubscription_WithNoActiveSubscription_ShouldReturnNotFound()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedAdminAsync();

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/subscriptions/current");

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.NotFound);
    }

    #endregion

    #region GetSubscriptionHistory Tests

    [Fact]
    public async Task GetSubscriptionHistory_WithValidToken_ShouldReturnSubscriptionHistory()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedAdminAsync();

        // Create multiple subscriptions for the organization
        var subscriptions = new List<Domain.Entities.Subscription>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                Plan = SubscriptionPlan.Basic,
                Status = SubscriptionStatus.Cancelled,
                StartDate = DateTime.UtcNow.AddMonths(-2),
                EndDate = DateTime.UtcNow.AddMonths(-1),
                CancelledAt = DateTime.UtcNow.AddMonths(-1),
                CreatedAt = DateTime.UtcNow.AddMonths(-2),
                MonthlyPrice = 29.99m,
                Currency = "USD",
                MaxTranscriptionMinutes = 1000
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                Plan = SubscriptionPlan.Professional,
                Status = SubscriptionStatus.Active,
                StartDate = DateTime.UtcNow.AddMonths(-1),
                CreatedAt = DateTime.UtcNow.AddMonths(-1),
                MonthlyPrice = 59.99m,
                Currency = "USD",
                MaxTranscriptionMinutes = 5000
            }
        };

        DbContext.Subscriptions.AddRange(subscriptions);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/subscriptions/history");

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.OK);

        var result = await ReadJsonResponseAsync<List<SubscriptionResponse>>(response);
        result.Should().NotBeNull();
        result!.Count.Should().Be(2);

        var cancelledSubscription = result.FirstOrDefault(s => s.Status == SubscriptionStatus.Cancelled);
        cancelledSubscription.Should().NotBeNull();
        cancelledSubscription!.Plan.Should().Be(SubscriptionPlan.Basic);
        cancelledSubscription.IsCancelled.Should().BeTrue();

        var activeSubscription = result.FirstOrDefault(s => s.Status == SubscriptionStatus.Active);
        activeSubscription.Should().NotBeNull();
        activeSubscription!.Plan.Should().Be(SubscriptionPlan.Professional);
        activeSubscription.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetSubscriptionHistory_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange - No authentication headers set

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/subscriptions/history");

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Unauthorized);
    }

    #endregion

    #region CreateSubscription Tests

    [Fact]
    public async Task CreateSubscription_WithAdminRole_ShouldCreateSubscription()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedAdminAsync();

        var request = new CreateSubscriptionRequest
        {
            Plan = SubscriptionPlan.Professional
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/subscriptions", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Created);

        var result = await ReadJsonResponseAsync<SubscriptionResponse>(response);
        result.Should().NotBeNull();
        result!.Plan.Should().Be(SubscriptionPlan.Professional);
        result.Status.Should().Be(SubscriptionStatus.Active);
        result.OrganizationId.Should().Be(organization.Id);
        result.StartDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.IsActive.Should().BeTrue();

        // Verify subscription was created in database
        var subscription = await DbContext.Subscriptions
            .FirstOrDefaultAsync(s => s.OrganizationId == organization.Id && s.Status == SubscriptionStatus.Active);
        subscription.Should().NotBeNull();
        subscription!.Plan.Should().Be(SubscriptionPlan.Professional);
    }

    [Fact]
    public async Task CreateSubscription_WithoutAdminRole_ShouldReturnForbidden()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedUserAsync(role: UserRole.OrganizationUser);

        var request = new CreateSubscriptionRequest
        {
            Plan = SubscriptionPlan.Professional
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/subscriptions", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateSubscription_WithExistingActiveSubscription_ShouldReturnBadRequest()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedAdminAsync();

        // Create an existing active subscription
        var existingSubscription = new Domain.Entities.Subscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Plan = SubscriptionPlan.Basic,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            MonthlyPrice = 29.99m,
            Currency = "USD",
            MaxTranscriptionMinutes = 1000
        };

        DbContext.Subscriptions.Add(existingSubscription);
        await DbContext.SaveChangesAsync();

        var request = new CreateSubscriptionRequest
        {
            Plan = SubscriptionPlan.Professional
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/subscriptions", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.BadRequest);
    }

    #endregion

    #region ChangeSubscriptionPlan Tests

    [Fact]
    public async Task ChangeSubscriptionPlan_WithAdminRole_ShouldChangePlan()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedAdminAsync();

        // Create an existing subscription
        var subscription = new Domain.Entities.Subscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Plan = SubscriptionPlan.Basic,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            MonthlyPrice = 29.99m,
            Currency = "USD",
            MaxTranscriptionMinutes = 1000
        };

        DbContext.Subscriptions.Add(subscription);
        await DbContext.SaveChangesAsync();

        var request = new ChangeSubscriptionPlanRequest
        {
            NewPlan = SubscriptionPlan.Professional
        };

        // Act
        var response = await HttpClient.PutAsync($"/api/v1.0/subscriptions/{subscription.Id}/plan", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.OK);

        var result = await ReadJsonResponseAsync<SubscriptionResponse>(response);
        result.Should().NotBeNull();
        result!.Plan.Should().Be(SubscriptionPlan.Professional);
        result.Id.Should().Be(subscription.Id);
        result.IsActive.Should().BeTrue();

        // Verify subscription was updated in database
        var updatedSubscription = await DbContext.Subscriptions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == subscription.Id);
        updatedSubscription.Should().NotBeNull();
        updatedSubscription!.Plan.Should().Be(SubscriptionPlan.Professional);
    }

    [Fact]
    public async Task ChangeSubscriptionPlan_WithoutAdminRole_ShouldReturnForbidden()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedUserAsync(role: UserRole.OrganizationUser);

        var subscription = new Domain.Entities.Subscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Plan = SubscriptionPlan.Basic,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            MonthlyPrice = 29.99m,
            Currency = "USD",
            MaxTranscriptionMinutes = 1000
        };

        DbContext.Subscriptions.Add(subscription);
        await DbContext.SaveChangesAsync();

        var request = new ChangeSubscriptionPlanRequest
        {
            NewPlan = SubscriptionPlan.Professional
        };

        // Act
        var response = await HttpClient.PutAsync($"/api/v1.0/subscriptions/{subscription.Id}/plan", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ChangeSubscriptionPlan_WithNonExistentSubscription_ShouldReturnNotFound()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedAdminAsync();
        var nonExistentId = Guid.NewGuid();

        var request = new ChangeSubscriptionPlanRequest
        {
            NewPlan = SubscriptionPlan.Professional
        };

        // Act
        var response = await HttpClient.PutAsync($"/api/v1.0/subscriptions/{nonExistentId}/plan", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ChangeSubscriptionPlan_WithCancelledSubscription_ShouldReturnBadRequest()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedAdminAsync();

        var subscription = new Domain.Entities.Subscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Plan = SubscriptionPlan.Basic,
            Status = SubscriptionStatus.Cancelled,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow,
            CancelledAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow.AddMonths(-1),
            MonthlyPrice = 29.99m,
            Currency = "USD",
            MaxTranscriptionMinutes = 1000
        };

        DbContext.Subscriptions.Add(subscription);
        await DbContext.SaveChangesAsync();

        var request = new ChangeSubscriptionPlanRequest
        {
            NewPlan = SubscriptionPlan.Professional
        };

        // Act
        var response = await HttpClient.PutAsync($"/api/v1.0/subscriptions/{subscription.Id}/plan", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.BadRequest);
    }

    #endregion

    #region CancelSubscription Tests

    [Fact]
    public async Task CancelSubscription_WithAdminRole_ShouldCancelSubscription()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedAdminAsync();

        var subscription = new Domain.Entities.Subscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Plan = SubscriptionPlan.Basic,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            MonthlyPrice = 29.99m,
            Currency = "USD",
            MaxTranscriptionMinutes = 1000
        };

        DbContext.Subscriptions.Add(subscription);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await HttpClient.PostAsync($"/api/v1.0/subscriptions/{subscription.Id}/cancel", null);

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.OK);

        var result = await ReadJsonResponseAsync<SubscriptionResponse>(response);
        result.Should().NotBeNull();
        result!.Status.Should().Be(SubscriptionStatus.Cancelled);
        result.Id.Should().Be(subscription.Id);
        result.IsCancelled.Should().BeTrue();
        result.CancelledAt.Should().NotBeNull();

        // Verify subscription was cancelled in database
        var cancelledSubscription = await DbContext.Subscriptions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == subscription.Id);
        cancelledSubscription.Should().NotBeNull();
        cancelledSubscription!.Status.Should().Be(SubscriptionStatus.Cancelled);
        cancelledSubscription.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelSubscription_WithoutAdminRole_ShouldReturnForbidden()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedUserAsync(role: UserRole.OrganizationUser);

        var subscription = new Domain.Entities.Subscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Plan = SubscriptionPlan.Basic,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            MonthlyPrice = 29.99m,
            Currency = "USD",
            MaxTranscriptionMinutes = 1000
        };

        DbContext.Subscriptions.Add(subscription);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await HttpClient.PostAsync($"/api/v1.0/subscriptions/{subscription.Id}/cancel", null);

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CancelSubscription_WithAlreadyCancelledSubscription_ShouldReturnBadRequest()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedAdminAsync();

        var subscription = new Domain.Entities.Subscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Plan = SubscriptionPlan.Basic,
            Status = SubscriptionStatus.Cancelled,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow,
            CancelledAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow.AddMonths(-1),
            MonthlyPrice = 29.99m,
            Currency = "USD",
            MaxTranscriptionMinutes = 1000
        };

        DbContext.Subscriptions.Add(subscription);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await HttpClient.PostAsync($"/api/v1.0/subscriptions/{subscription.Id}/cancel", null);

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.BadRequest);
    }

    #endregion

    #region ReactivateSubscription Tests

    [Fact]
    public async Task ReactivateSubscription_WithAdminRole_ShouldReactivateSubscription()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedAdminAsync();

        var subscription = new Domain.Entities.Subscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Plan = SubscriptionPlan.Basic,
            Status = SubscriptionStatus.Cancelled,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow,
            CancelledAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow.AddMonths(-1),
            MonthlyPrice = 29.99m,
            Currency = "USD",
            MaxTranscriptionMinutes = 1000
        };

        DbContext.Subscriptions.Add(subscription);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await HttpClient.PostAsync($"/api/v1.0/subscriptions/{subscription.Id}/reactivate", null);

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.OK);

        var result = await ReadJsonResponseAsync<SubscriptionResponse>(response);
        result.Should().NotBeNull();
        result!.Status.Should().Be(SubscriptionStatus.Active);
        result.Id.Should().Be(subscription.Id);
        result.IsActive.Should().BeTrue();
        result.CancelledAt.Should().BeNull();

        // Verify subscription was reactivated in database
        var reactivatedSubscription = await DbContext.Subscriptions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == subscription.Id);
        reactivatedSubscription.Should().NotBeNull();
        reactivatedSubscription!.Status.Should().Be(SubscriptionStatus.Active);
        reactivatedSubscription.CancelledAt.Should().BeNull();
    }

    [Fact]
    public async Task ReactivateSubscription_WithoutAdminRole_ShouldReturnForbidden()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedUserAsync(role: UserRole.OrganizationUser);

        var subscription = new Domain.Entities.Subscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Plan = SubscriptionPlan.Basic,
            Status = SubscriptionStatus.Cancelled,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow,
            CancelledAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow.AddMonths(-1),
            MonthlyPrice = 29.99m,
            Currency = "USD",
            MaxTranscriptionMinutes = 1000
        };

        DbContext.Subscriptions.Add(subscription);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await HttpClient.PostAsync($"/api/v1.0/subscriptions/{subscription.Id}/reactivate", null);

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReactivateSubscription_WithAlreadyActiveSubscription_ShouldReturnBadRequest()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedAdminAsync();

        var subscription = new Domain.Entities.Subscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Plan = SubscriptionPlan.Basic,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            MonthlyPrice = 29.99m,
            Currency = "USD",
            MaxTranscriptionMinutes = 1000
        };

        DbContext.Subscriptions.Add(subscription);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await HttpClient.PostAsync($"/api/v1.0/subscriptions/{subscription.Id}/reactivate", null);

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetUsageAnalytics Tests

    [Fact]
    public async Task GetUsageAnalytics_WithValidToken_ShouldReturnUsageAnalytics()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedAdminAsync();

        // Create an active subscription
        var subscription = new Domain.Entities.Subscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Plan = SubscriptionPlan.Professional,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            MonthlyPrice = 59.99m,
            Currency = "USD",
            MaxTranscriptionMinutes = 5000,
            TranscriptionMinutesUsed = 1500,
            UsageResetDate = DateTime.UtcNow.AddDays(15)
        };

        DbContext.Subscriptions.Add(subscription);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/subscriptions/analytics");

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.OK);

        var result = await ReadJsonResponseAsync<SubscriptionUsageResponse>(response);
        result.Should().NotBeNull();
        result!.OrganizationId.Should().Be(organization.Id);
        result.CurrentPlan.Should().Be(SubscriptionPlan.Professional);
        result.PlanName.Should().NotBeNullOrEmpty();
        result.MonthlyLimit.Should().Be(5000);
        result.MinutesUsed.Should().Be(1500);
        result.MinutesRemaining.Should().Be(3500);
        result.UsagePercentage.Should().Be(30.0m); // 1500/5000 * 100
        result.IsOverLimit.Should().BeFalse();
        result.IsAtLimit.Should().BeFalse();
        result.DaysUntilReset.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetUsageAnalytics_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange - No authentication headers set

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/subscriptions/analytics");

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUsageAnalytics_WithNoActiveSubscription_ShouldReturnNotFound()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedAdminAsync();

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/subscriptions/analytics");

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.NotFound);
    }

    #endregion

    #region CheckTranscriptionLimit Tests

    [Fact]
    public async Task CheckTranscriptionLimit_WithValidToken_ShouldReturnLimitCheck()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedAdminAsync();

        // Create an active subscription
        var subscription = new Domain.Entities.Subscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Plan = SubscriptionPlan.Basic,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            MonthlyPrice = 29.99m,
            Currency = "USD",
            MaxTranscriptionMinutes = 1000,
            TranscriptionMinutesUsed = 800
        };

        DbContext.Subscriptions.Add(subscription);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/subscriptions/limits/transcription?minutes=100");

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.OK);

        var result = await ReadJsonResponseAsync<Dictionary<string, object>>(response);
        result.Should().NotBeNull();
        result!.Should().ContainKey("canUseMinutes");
        result.Should().ContainKey("requestedMinutes");
        ((JsonElement)result["canUseMinutes"]).GetBoolean().Should().BeTrue(); // 800 + 100 = 900 < 1000
        ((JsonElement)result["requestedMinutes"]).GetInt32().Should().Be(100);
    }

    [Fact]
    public async Task CheckTranscriptionLimit_WithExceedingLimit_ShouldReturnFalse()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedAdminAsync();

        // Create an active subscription
        var subscription = new Domain.Entities.Subscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Plan = SubscriptionPlan.Basic,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            MonthlyPrice = 29.99m,
            Currency = "USD",
            MaxTranscriptionMinutes = 1000,
            TranscriptionMinutesUsed = 950
        };

        DbContext.Subscriptions.Add(subscription);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/subscriptions/limits/transcription?minutes=100");

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.OK);

        var result = await ReadJsonResponseAsync<Dictionary<string, object>>(response);
        result.Should().NotBeNull();
        result!.Should().ContainKey("canUseMinutes");
        result.Should().ContainKey("requestedMinutes");
        ((JsonElement)result["canUseMinutes"]).GetBoolean().Should().BeFalse(); // 950 + 100 = 1050 > 1000
        ((JsonElement)result["requestedMinutes"]).GetInt32().Should().Be(100);
    }

    [Fact]
    public async Task CheckTranscriptionLimit_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange - No authentication headers set

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/subscriptions/limits/transcription?minutes=100");

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Unauthorized);
    }

    #endregion
}
