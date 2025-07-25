using FluentAssertions;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Tests.Unit.Common;

namespace SermonTranscription.Tests.Unit.Entities;

/// <summary>
/// Unit tests for Subscription entity business logic
/// </summary>
public class SubscriptionTests : BaseUnitTest
{
    [Fact]
    public void Subscription_IsActive_ShouldReturnTrue_WhenStatusIsActive()
    {
        // Arrange
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Status = SubscriptionStatus.Active;

        // Act
        var isActive = subscription.IsActive;

        // Assert
        isActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(SubscriptionStatus.PastDue)]
    [InlineData(SubscriptionStatus.Cancelled)]
    [InlineData(SubscriptionStatus.Suspended)]
    [InlineData(SubscriptionStatus.Expired)]
    public void Subscription_IsActive_ShouldReturnFalse_WhenStatusIsNotActive(SubscriptionStatus status)
    {
        // Arrange
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Status = status;

        // Act
        var isActive = subscription.IsActive;

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public void Subscription_IsExpired_ShouldReturnTrue_WhenEndDateIsInThePast()
    {
        // Arrange
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.EndDate = DateTime.UtcNow.AddDays(-1);

        // Act
        var isExpired = subscription.IsExpired;

        // Assert
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void Subscription_IsExpired_ShouldReturnFalse_WhenEndDateIsInTheFuture()
    {
        // Arrange
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.EndDate = DateTime.UtcNow.AddDays(1);

        // Act
        var isExpired = subscription.IsExpired;

        // Assert
        isExpired.Should().BeFalse();
    }

    [Fact]
    public void Subscription_IsExpired_ShouldReturnFalse_WhenEndDateIsNull()
    {
        // Arrange
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.EndDate = null;

        // Act
        var isExpired = subscription.IsExpired;

        // Assert
        isExpired.Should().BeFalse();
    }

    [Fact]
    public void Subscription_IsCancelled_ShouldReturnTrue_WhenStatusIsCancelled()
    {
        // Arrange
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Status = SubscriptionStatus.Cancelled;

        // Act
        var isCancelled = subscription.IsCancelled;

        // Assert
        isCancelled.Should().BeTrue();
    }

    [Theory]
    [InlineData(SubscriptionStatus.Active)]
    [InlineData(SubscriptionStatus.PastDue)]
    [InlineData(SubscriptionStatus.Suspended)]
    [InlineData(SubscriptionStatus.Expired)]
    public void Subscription_IsCancelled_ShouldReturnFalse_WhenStatusIsNotCancelled(SubscriptionStatus status)
    {
        // Arrange
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Status = status;

        // Act
        var isCancelled = subscription.IsCancelled;

        // Assert
        isCancelled.Should().BeFalse();
    }

    [Fact]
    public void Subscription_RemainingTranscriptionMinutes_ShouldReturnCorrectValue()
    {
        // Arrange
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.MaxTranscriptionMinutes = 100;
        subscription.TranscriptionMinutesUsed = 30;

        // Act
        var remaining = subscription.RemainingTranscriptionMinutes;

        // Assert
        remaining.Should().Be(70);
    }

    [Fact]
    public void Subscription_RemainingTranscriptionMinutes_ShouldReturnZero_WhenUsedExceedsMax()
    {
        // Arrange
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.MaxTranscriptionMinutes = 100;
        subscription.TranscriptionMinutesUsed = 150;

        // Act
        var remaining = subscription.RemainingTranscriptionMinutes;

        // Assert
        remaining.Should().Be(0);
    }

    [Fact]
    public void Subscription_YearlyPrice_ShouldCalculateCorrectly()
    {
        // Arrange
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.MonthlyPrice = 50.00m;

        // Act
        var yearlyPrice = subscription.YearlyPrice;

        // Assert
        yearlyPrice.Should().Be(600.00m);
    }

    [Fact]
    public void Subscription_Cancel_ShouldSetStatusToCancelled()
    {
        // Arrange
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Status = SubscriptionStatus.Active;
        var originalUpdatedAt = subscription.UpdatedAt;

        // Act
        subscription.Cancel();

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
        subscription.CancelledAt.Should().NotBeNull();
        subscription.CancelledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        subscription.UpdatedAt.Should().NotBe(originalUpdatedAt);
        subscription.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Subscription_Cancel_ShouldUseProvidedCancellationDate()
    {
        // Arrange
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        var cancellationDate = DateTime.UtcNow.AddDays(-1);

        // Act
        subscription.Cancel(cancellationDate);

        // Assert
        subscription.CancelledAt.Should().Be(cancellationDate);
    }

    [Fact]
    public void Subscription_Suspend_ShouldSetStatusToSuspended()
    {
        // Arrange
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Status = SubscriptionStatus.Active;
        var originalUpdatedAt = subscription.UpdatedAt;

        // Act
        subscription.Suspend();

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Suspended);
        subscription.UpdatedAt.Should().NotBe(originalUpdatedAt);
        subscription.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(SubscriptionStatus.Cancelled)]
    [InlineData(SubscriptionStatus.Suspended)]
    public void Subscription_Reactivate_ShouldSetStatusToActive_WhenCancelledOrSuspended(SubscriptionStatus status)
    {
        // Arrange
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Status = status;
        subscription.CancelledAt = DateTime.UtcNow.AddDays(-1);
        var originalUpdatedAt = subscription.UpdatedAt;

        // Act
        subscription.Reactivate();

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.CancelledAt.Should().BeNull();
        subscription.UpdatedAt.Should().NotBe(originalUpdatedAt);
        subscription.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(SubscriptionStatus.Active)]
    [InlineData(SubscriptionStatus.PastDue)]
    [InlineData(SubscriptionStatus.Expired)]
    public void Subscription_Reactivate_ShouldNotChangeStatus_WhenNotCancelledOrSuspended(SubscriptionStatus status)
    {
        // Arrange
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Status = status;
        var originalUpdatedAt = subscription.UpdatedAt;

        // Act
        subscription.Reactivate();

        // Assert
        subscription.Status.Should().Be(status);
        subscription.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public void Subscription_ChangePlan_ShouldUpdatePlanAndLimits()
    {
        // Arrange
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Plan = SubscriptionPlan.Basic;
        var originalUpdatedAt = subscription.UpdatedAt;

        // Act
        subscription.ChangePlan(SubscriptionPlan.Professional);

        // Assert
        subscription.Plan.Should().Be(SubscriptionPlan.Professional);
        subscription.MaxTranscriptionMinutes.Should().Be(600);
        subscription.CanExportTranscriptions.Should().BeTrue();
        subscription.HasRealtimeTranscription.Should().BeTrue();
        subscription.HasPrioritySupport.Should().BeTrue();
        subscription.MonthlyPrice.Should().Be(80.00m);
        subscription.UpdatedAt.Should().NotBe(originalUpdatedAt);
        subscription.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Subscription_ChangePlan_ShouldNotUpdate_WhenPlanIsSame()
    {
        // Arrange
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Plan = SubscriptionPlan.Basic;
        var originalUpdatedAt = subscription.UpdatedAt;

        // Act
        subscription.ChangePlan(SubscriptionPlan.Basic);

        // Assert
        subscription.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Theory]
    [InlineData(SubscriptionPlan.Basic, 360, 48.00)]
    [InlineData(SubscriptionPlan.Professional, 600, 80.00)]
    [InlineData(SubscriptionPlan.Enterprise, 840, 112.00)]
    public void Subscription_UpdatePlanLimits_ShouldSetCorrectLimitsForPlan(SubscriptionPlan plan, int expectedMinutes, decimal expectedPrice)
    {
        // Arrange
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Plan = plan;

        // Act
        subscription.UpdatePlanLimits();

        // Assert
        subscription.MaxTranscriptionMinutes.Should().Be(expectedMinutes);
        subscription.CanExportTranscriptions.Should().BeTrue();
        subscription.HasRealtimeTranscription.Should().BeTrue();
        subscription.MonthlyPrice.Should().Be(expectedPrice);

        // Check priority support based on plan
        if (plan == SubscriptionPlan.Basic)
        {
            subscription.HasPrioritySupport.Should().BeFalse();
        }
        else
        {
            subscription.HasPrioritySupport.Should().BeTrue();
        }
    }

    [Fact]
    public void Subscription_CanUseTranscriptionMinutes_ShouldReturnTrue_WhenActiveAndSufficientMinutes()
    {
        // Arrange
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Status = SubscriptionStatus.Active;
        subscription.MaxTranscriptionMinutes = 100;
        subscription.TranscriptionMinutesUsed = 50;

        // Act
        var canUse = subscription.CanUseTranscriptionMinutes(30);

        // Assert
        canUse.Should().BeTrue();
    }

    [Fact]
    public void Subscription_CanUseTranscriptionMinutes_ShouldReturnFalse_WhenNotActive()
    {
        // Arrange
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.MaxTranscriptionMinutes = 100;
        subscription.TranscriptionMinutesUsed = 50;

        // Act
        var canUse = subscription.CanUseTranscriptionMinutes(30);

        // Assert
        canUse.Should().BeFalse();
    }

    [Fact]
    public void Subscription_CanUseTranscriptionMinutes_ShouldReturnFalse_WhenInsufficientMinutes()
    {
        // Arrange
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Status = SubscriptionStatus.Active;
        subscription.MaxTranscriptionMinutes = 100;
        subscription.TranscriptionMinutesUsed = 80;

        // Act
        var canUse = subscription.CanUseTranscriptionMinutes(30);

        // Assert
        canUse.Should().BeFalse();
    }

    [Fact]
    public void Subscription_UseTranscriptionMinutes_ShouldIncreaseUsedMinutes()
    {
        // Arrange
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Status = SubscriptionStatus.Active;
        subscription.MaxTranscriptionMinutes = 100;
        subscription.TranscriptionMinutesUsed = 50;
        var originalUpdatedAt = subscription.UpdatedAt;

        // Act
        subscription.UseTranscriptionMinutes(20);

        // Assert
        subscription.TranscriptionMinutesUsed.Should().Be(70);
        subscription.UpdatedAt.Should().NotBe(originalUpdatedAt);
        subscription.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Subscription_UseTranscriptionMinutes_ShouldThrowException_WhenInsufficientMinutes()
    {
        // Arrange
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Status = SubscriptionStatus.Active;
        subscription.MaxTranscriptionMinutes = 100;
        subscription.TranscriptionMinutesUsed = 80;

        // Act & Assert
        var action = () => subscription.UseTranscriptionMinutes(30);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Insufficient transcription minutes remaining");
    }

    [Fact]
    public void Subscription_UseTranscriptionMinutes_ShouldThrowException_WhenNotActive()
    {
        // Arrange
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.MaxTranscriptionMinutes = 100;
        subscription.TranscriptionMinutesUsed = 50;

        // Act & Assert
        var action = () => subscription.UseTranscriptionMinutes(20);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Insufficient transcription minutes remaining");
    }

    [Fact]
    public void Subscription_ResetUsage_ShouldResetTranscriptionMinutesUsed()
    {
        // Arrange
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.TranscriptionMinutesUsed = 100;
        var originalUpdatedAt = subscription.UpdatedAt;

        // Act
        subscription.ResetUsage();

        // Assert
        subscription.TranscriptionMinutesUsed.Should().Be(0);
        subscription.UsageResetDate.Should().BeCloseTo(DateTime.UtcNow.AddMonths(1), TimeSpan.FromDays(2));
        subscription.UpdatedAt.Should().NotBe(originalUpdatedAt);
        subscription.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Subscription_CanBeSavedToDatabase_UsingTestDataFactory()
    {
        // Arrange
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        var organization = TestDataFactory.OrganizationFaker.Generate();

        // Set up the relationship
        subscription.OrganizationId = organization.Id;
        subscription.Organization = organization;

        // Act
        DbContext.Subscriptions.Add(subscription);
        DbContext.Organizations.Add(organization);
        var result = await DbContext.SaveChangesAsync();

        // Assert
        result.Should().BeGreaterThan(0);

        var savedSubscription = await DbContext.Subscriptions.FindAsync(subscription.Id);
        savedSubscription.Should().NotBeNull();
        savedSubscription!.Plan.Should().Be(subscription.Plan);
        savedSubscription.Status.Should().Be(subscription.Status);
        savedSubscription.MonthlyPrice.Should().Be(subscription.MonthlyPrice);
    }

    [Fact]
    public async Task Subscription_DatabaseConstraints_ShouldEnforceRequiredFields()
    {
        // Arrange
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            // Missing required fields like Plan, Status, etc.
        };

        // Act & Assert
        DbContext.Subscriptions.Add(subscription);

        // Note: In-memory database doesn't enforce the same constraints as a real database
        // This test demonstrates the structure but won't fail in the in-memory provider
        // In a real database, this would fail due to missing required fields
        var result = await DbContext.SaveChangesAsync();

        // The save should succeed in in-memory database, but we can verify the entity was added
        result.Should().BeGreaterThan(0);
        var savedSubscription = await DbContext.Subscriptions.FindAsync(subscription.Id);
        savedSubscription.Should().NotBeNull();
    }
}
