using FluentAssertions;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Exceptions;
using SermonTranscription.Tests.Unit.Common;

namespace SermonTranscription.Tests.Unit.Entities;

/// <summary>
/// Unit tests for enhanced Organization entity with business rules and validation
/// </summary>
public class OrganizationEnhancedTests : BaseUnitTest
{
    [Fact]
    public void Organization_Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.IsActive = true;

        // Act
        organization.Deactivate();

        // Assert
        organization.IsActive.Should().BeFalse();
        organization.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Organization_Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.IsActive = false;

        // Act
        organization.Activate();

        // Assert
        organization.IsActive.Should().BeTrue();
        organization.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Organization_HasActiveSubscription_ShouldReturnTrue_WhenActiveSubscriptionExists()
    {
        // Arrange
        var organization = TestDataFactory.OrganizationFaker.Generate();
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Status = Domain.Enums.SubscriptionStatus.Active;
        organization.Subscriptions.Add(subscription);

        // Act
        var hasActive = organization.HasActiveSubscription();

        // Assert
        hasActive.Should().BeTrue();
    }

    [Fact]
    public void Organization_HasActiveSubscription_ShouldReturnFalse_WhenNoActiveSubscription()
    {
        // Arrange
        var organization = TestDataFactory.OrganizationFaker.Generate();
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Status = Domain.Enums.SubscriptionStatus.Cancelled;
        organization.Subscriptions.Add(subscription);

        // Act
        var hasActive = organization.HasActiveSubscription();

        // Assert
        hasActive.Should().BeFalse();
    }

    [Fact]
    public void Organization_GetCurrentSubscription_ShouldReturnActiveSubscription()
    {
        // Arrange
        var organization = TestDataFactory.OrganizationFaker.Generate();
        var activeSubscription = TestDataFactory.SubscriptionFaker.Generate();
        activeSubscription.Status = Domain.Enums.SubscriptionStatus.Active;
        var cancelledSubscription = TestDataFactory.SubscriptionFaker.Generate();
        cancelledSubscription.Status = Domain.Enums.SubscriptionStatus.Cancelled;

        organization.Subscriptions.Add(activeSubscription);
        organization.Subscriptions.Add(cancelledSubscription);

        // Act
        var current = organization.GetCurrentSubscription();

        // Assert
        current.Should().Be(activeSubscription);
    }

    [Fact]
    public void Organization_GetActiveUserCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var organization = TestDataFactory.OrganizationFaker.Generate();
        var activeUser1 = TestDataFactory.UserFaker.Generate();
        activeUser1.IsActive = true;
        var activeUser2 = TestDataFactory.UserFaker.Generate();
        activeUser2.IsActive = true;
        var inactiveUser = TestDataFactory.UserFaker.Generate();
        inactiveUser.IsActive = false;

        // Create UserOrganization entities and add them to the organization
        var userOrg1 = new UserOrganization
        {
            UserId = activeUser1.Id,
            OrganizationId = organization.Id,
            Role = UserRole.OrganizationUser,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var userOrg2 = new UserOrganization
        {
            UserId = activeUser2.Id,
            OrganizationId = organization.Id,
            Role = UserRole.OrganizationUser,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var userOrg3 = new UserOrganization
        {
            UserId = inactiveUser.Id,
            OrganizationId = organization.Id,
            Role = UserRole.OrganizationUser,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        organization.UserOrganizations.Add(userOrg1);
        organization.UserOrganizations.Add(userOrg2);
        organization.UserOrganizations.Add(userOrg3);

        // Act
        var count = organization.GetActiveUserCount();

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public void Organization_UpdateSubscriptionLimits_ShouldUpdateAllLimits()
    {
        // Arrange
        var organization = TestDataFactory.OrganizationFaker.Generate();
        var originalMaxMinutes = organization.MaxTranscriptionMinutes;
        var originalCanExport = organization.CanExportTranscriptions;
        var originalHasRealtime = organization.HasRealtimeTranscription;

        // Act
        organization.UpdateSubscriptionLimits(3000, true, false);

        // Assert
        organization.MaxTranscriptionMinutes.Should().Be(3000);
        organization.CanExportTranscriptions.Should().BeTrue();
        organization.HasRealtimeTranscription.Should().BeFalse();
        organization.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Organization_HasRealtimeTranscriptionEnabled_ShouldReturnTrue_WhenAllConditionsMet()
    {
        // Arrange
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.IsActive = true;
        organization.HasRealtimeTranscription = true;
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Status = Domain.Enums.SubscriptionStatus.Active;
        organization.Subscriptions.Add(subscription);

        // Act
        var hasRealtime = organization.HasRealtimeTranscriptionEnabled();

        // Assert
        hasRealtime.Should().BeTrue();
    }

    [Fact]
    public void Organization_HasRealtimeTranscriptionEnabled_ShouldReturnFalse_WhenInactive()
    {
        // Arrange
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.IsActive = false;
        organization.HasRealtimeTranscription = true;

        // Act
        var hasRealtime = organization.HasRealtimeTranscriptionEnabled();

        // Assert
        hasRealtime.Should().BeFalse();
    }

    [Fact]
    public void Organization_CanExportTranscriptionsEnabled_ShouldReturnTrue_WhenAllConditionsMet()
    {
        // Arrange
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.IsActive = true;
        organization.CanExportTranscriptions = true;
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Status = Domain.Enums.SubscriptionStatus.Active;
        organization.Subscriptions.Add(subscription);

        // Act
        var canExport = organization.CanExportTranscriptionsEnabled();

        // Assert
        canExport.Should().BeTrue();
    }

    [Fact]
    public void Organization_GetFullAddress_ShouldReturnFormattedAddress()
    {
        // Arrange
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.Address = "123 Main St";
        organization.City = "Anytown";
        organization.State = "CA";
        organization.PostalCode = "12345";
        organization.Country = "USA";

        // Act
        var fullAddress = organization.GetFullAddress();

        // Assert
        fullAddress.Should().Be("123 Main St, Anytown, CA, 12345, USA");
    }

    [Fact]
    public void Organization_GetFullAddress_ShouldReturnPartialAddress_WhenSomeFieldsAreNull()
    {
        // Arrange
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.Address = "123 Main St";
        organization.City = "Anytown";
        organization.State = null;
        organization.PostalCode = "12345";
        organization.Country = null;

        // Act
        var fullAddress = organization.GetFullAddress();

        // Assert
        fullAddress.Should().Be("123 Main St, Anytown, 12345");
    }

    [Fact]
    public void Organization_HasCompleteContactInfo_ShouldReturnTrue_WhenEmailAndPhoneProvided()
    {
        // Arrange
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.ContactEmail = "test@example.com";
        organization.PhoneNumber = "555-1234";

        // Act
        var hasComplete = organization.HasCompleteContactInfo();

        // Assert
        hasComplete.Should().BeTrue();
    }

    [Fact]
    public void Organization_HasCompleteContactInfo_ShouldReturnFalse_WhenEmailMissing()
    {
        // Arrange
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.ContactEmail = null;
        organization.PhoneNumber = "555-1234";

        // Act
        var hasComplete = organization.HasCompleteContactInfo();

        // Assert
        hasComplete.Should().BeFalse();
    }

    [Fact]
    public void Organization_UpdateLogo_ShouldUpdateLogoUrl()
    {
        // Arrange
        var organization = TestDataFactory.OrganizationFaker.Generate();
        var newLogoUrl = "https://example.com/new-logo.png";

        // Act
        organization.UpdateLogo(newLogoUrl);

        // Assert
        organization.LogoUrl.Should().Be(newLogoUrl);
        organization.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Organization_UpdateWebsite_ShouldUpdateWebsiteUrl()
    {
        // Arrange
        var organization = TestDataFactory.OrganizationFaker.Generate();
        var newWebsiteUrl = "https://newwebsite.com";

        // Act
        organization.UpdateWebsite(newWebsiteUrl);

        // Assert
        organization.WebsiteUrl.Should().Be(newWebsiteUrl);
        organization.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Organization_ValidateIsActive_ShouldThrowException_WhenInactive()
    {
        // Arrange
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.IsActive = false;

        // Act & Assert
        var act = () => organization.ValidateIsActive();
        act.Should().Throw<OrganizationInactiveException>()
           .WithMessage($"Organization {organization.Name} is not active.");
    }

    [Fact]
    public void Organization_ValidateCanCreateTranscription_ShouldThrowException_WhenRealtimeDisabled()
    {
        // Arrange
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.IsActive = true;
        organization.HasRealtimeTranscription = false;
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Status = Domain.Enums.SubscriptionStatus.Active;
        organization.Subscriptions.Add(subscription);

        // Act & Assert
        var act = () => organization.ValidateCanCreateTranscription();
        act.Should().Throw<OrganizationFeatureNotAvailableException>()
           .WithMessage($"Organization {organization.Name} does not have real-time transcription enabled.");
    }

    [Fact]
    public void Organization_ValidateCanExportTranscriptions_ShouldThrowException_WhenExportDisabled()
    {
        // Arrange
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.IsActive = true;
        organization.CanExportTranscriptions = false;
        var subscription = TestDataFactory.SubscriptionFaker.Generate();
        subscription.Status = Domain.Enums.SubscriptionStatus.Active;
        organization.Subscriptions.Add(subscription);

        // Act & Assert
        var act = () => organization.ValidateCanExportTranscriptions();
        act.Should().Throw<OrganizationFeatureNotAvailableException>()
           .WithMessage($"Organization {organization.Name} does not have transcription export enabled.");
    }

    [Fact]
    public void Organization_ValidateHasRealtimeTranscription_ShouldThrowException_WhenInactive()
    {
        // Arrange
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.IsActive = false;
        organization.HasRealtimeTranscription = true;

        // Act & Assert
        var act = () => organization.ValidateHasRealtimeTranscription();
        act.Should().Throw<OrganizationInactiveException>()
           .WithMessage($"Organization {organization.Name} is not active.");
    }
}
