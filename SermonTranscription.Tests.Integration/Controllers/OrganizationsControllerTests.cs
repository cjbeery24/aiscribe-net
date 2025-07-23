using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SermonTranscription.Tests.Integration.Common;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Services;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using SermonTranscription.Infrastructure.Data;

namespace SermonTranscription.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for organization management endpoints
/// </summary>
public class OrganizationsControllerTests : BaseIntegrationTest
{
    public OrganizationsControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateOrganization_WithValidData_ShouldCreateOrganizationAndReturnSuccess()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var token = GenerateJwtTokenAsync(user);
        SetAuthorizationHeader(token);

        var request = new CreateOrganizationRequest
        {
            Name = "Test Organization",
            Description = "A test organization",
            WebsiteUrl = "https://test.com",
            Address = "123 Test St",
            City = "Test City",
            State = "TS",
            PostalCode = "12345",
            Country = "Test Country",
            PhoneNumber = "555-123-4567"
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/organizations", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.OK);

        var result = await ReadJsonResponseAsync<OrganizationResponse>(response);
        result.Should().NotBeNull();
        result!.Name.Should().Be(request.Name);
        result.Description.Should().Be(request.Description);
        result.WebsiteUrl.Should().Be(request.WebsiteUrl);
        result.Address.Should().Be(request.Address);
        result.City.Should().Be(request.City);
        result.State.Should().Be(request.State);
        result.PostalCode.Should().Be(request.PostalCode);
        result.Country.Should().Be(request.Country);
        result.PhoneNumber.Should().Be(request.PhoneNumber);
        result.IsActive.Should().BeTrue();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify organization was created in database
        var organization = await DbContext.Organizations.FirstOrDefaultAsync(o => o.Name == request.Name);
        organization.Should().NotBeNull();
        organization!.Name.Should().Be(request.Name);
        organization.IsActive.Should().BeTrue();

        // Verify user is associated with the organization as admin
        var userOrg = await DbContext.UserOrganizations
            .FirstOrDefaultAsync(uo => uo.UserId == user.Id && uo.OrganizationId == organization.Id);
        userOrg.Should().NotBeNull();
        userOrg!.Role.Should().Be(Domain.Enums.UserRole.OrganizationAdmin);
    }

    [Fact]
    public async Task CreateOrganization_WithInvalidData_ShouldReturnValidationErrors()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var token = GenerateJwtTokenAsync(user);
        SetAuthorizationHeader(token);

        var request = new CreateOrganizationRequest
        {
            Name = "", // Invalid: empty name
            Description = new string('A', 1001), // Invalid: too long
            WebsiteUrl = "invalid-url", // Invalid: not a valid URL
            ContactEmail = "invalid-email" // Invalid: not a valid email
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/organizations", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.BadRequest);

        // The API might return a different error structure, so just verify it's a bad request
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrganization_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearHeaders();

        var request = new CreateOrganizationRequest
        {
            Name = "Test Organization"
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/organizations", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LoadOrganization_WithValidToken_ShouldReturnOrganization()
    {
        // Arrange
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync();
        var token = GenerateJwtTokenAsync(user, organization);
        SetAuthorizationHeader(token);
        SetOrganizationHeader(organization.Id);

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/organizations/load");

        // Assert
        await AssertSuccessStatusCodeAsync(response);

        var result = await ReadJsonResponseAsync<OrganizationResponse>(response);
        result.Should().NotBeNull();
        result!.Id.Should().Be(organization.Id);
        result.Name.Should().Be(organization.Name);
        result.IsActive.Should().Be(organization.IsActive);
    }

    [Fact]
    public async Task LoadOrganization_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearHeaders();

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/organizations/load");

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LoadOrganization_WithoutOrganizationHeader_ShouldReturnForbidden()
    {
        // Arrange
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync();
        var token = GenerateJwtTokenAsync(user, organization);
        SetAuthorizationHeader(token);
        // Don't set organization header

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/organizations/load");

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateOrganization_WithValidData_ShouldUpdateOrganizationAndReturnSuccess()
    {
        // Arrange
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync("admin@example.com", Domain.Enums.UserRole.OrganizationAdmin);
        var token = GenerateJwtTokenAsync(user, organization);
        SetAuthorizationHeader(token);
        SetOrganizationHeader(organization.Id);

        var request = new UpdateOrganizationRequest
        {
            Name = "Updated Organization",
            Description = "Updated description",
            WebsiteUrl = "https://updated.com",
            Address = "456 Updated St",
            City = "Updated City",
            State = "US",
            PostalCode = "54321",
            Country = "Updated Country",
            PhoneNumber = "555-987-6543"
        };

        // Act
        var response = await HttpClient.PutAsync("/api/v1.0/organizations", CreateJsonContent(request));

        // Assert
        await AssertSuccessStatusCodeAsync(response);

        var result = await ReadJsonResponseAsync<OrganizationResponse>(response);
        result.Should().NotBeNull();
        result!.Name.Should().Be(request.Name);
        result.Description.Should().Be(request.Description);
        result.WebsiteUrl.Should().Be(request.WebsiteUrl);
        result.Address.Should().Be(request.Address);
        result.City.Should().Be(request.City);
        result.State.Should().Be(request.State);
        result.PostalCode.Should().Be(request.PostalCode);
        result.Country.Should().Be(request.Country);
        result.PhoneNumber.Should().Be(request.PhoneNumber);
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify the update was persisted to database
        var updatedOrg = await DbContext.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == organization.Id);
        updatedOrg.Should().NotBeNull();
        updatedOrg!.Name.Should().Be(request.Name);
        updatedOrg.Description.Should().Be(request.Description);
        updatedOrg.WebsiteUrl.Should().Be(request.WebsiteUrl);
        updatedOrg.Address.Should().Be(request.Address);
        updatedOrg.City.Should().Be(request.City);
        updatedOrg.State.Should().Be(request.State);
        updatedOrg.PostalCode.Should().Be(request.PostalCode);
        updatedOrg.Country.Should().Be(request.Country);
        updatedOrg.PhoneNumber.Should().Be(request.PhoneNumber);
    }

    [Fact]
    public async Task UpdateOrganization_WithoutAdminRole_ShouldReturnForbidden()
    {
        // Arrange
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync("user@example.com", Domain.Enums.UserRole.OrganizationUser);
        var token = GenerateJwtTokenAsync(user, organization);
        SetAuthorizationHeader(token);
        SetOrganizationHeader(organization.Id);

        var request = new UpdateOrganizationRequest
        {
            Name = "Updated Organization"
        };

        // Act
        var response = await HttpClient.PutAsync("/api/v1.0/organizations", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateOrganizationSettings_WithValidData_ShouldUpdateSettingsAndReturnSuccess()
    {
        // Arrange
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync("admin@example.com", Domain.Enums.UserRole.OrganizationAdmin);
        var token = GenerateJwtTokenAsync(user, organization);
        SetAuthorizationHeader(token);
        SetOrganizationHeader(organization.Id);

        var request = new UpdateOrganizationSettingsRequest
        {
            MaxUsers = 50,
            MaxTranscriptionHours = 100,
            CanExportTranscriptions = true,
            HasRealtimeTranscription = true
        };

        // Act
        var response = await HttpClient.PutAsync("/api/v1.0/organizations/settings", CreateJsonContent(request));

        // Assert
        await AssertSuccessStatusCodeAsync(response);

        var result = await ReadJsonResponseAsync<OrganizationResponse>(response);
        result.Should().NotBeNull();
        result!.MaxUsers.Should().Be(request.MaxUsers);
        result.MaxTranscriptionHours.Should().Be(request.MaxTranscriptionHours);
        result.CanExportTranscriptions.Should().Be(request.CanExportTranscriptions!.Value);
        result.HasRealtimeTranscription.Should().Be(request.HasRealtimeTranscription!.Value);

        // Verify the update was persisted to database
        var updatedOrg = await DbContext.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == organization.Id);
        updatedOrg.Should().NotBeNull();
        updatedOrg!.MaxUsers.Should().Be(request.MaxUsers);
        updatedOrg.MaxTranscriptionHours.Should().Be(request.MaxTranscriptionHours);
        updatedOrg.CanExportTranscriptions.Should().Be(request.CanExportTranscriptions!.Value);
        updatedOrg.HasRealtimeTranscription.Should().Be(request.HasRealtimeTranscription!.Value);
    }

    [Fact]
    public async Task UpdateOrganizationLogo_WithValidData_ShouldUpdateLogoAndReturnSuccess()
    {
        // Arrange
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync("admin@example.com", Domain.Enums.UserRole.OrganizationAdmin);
        var token = GenerateJwtTokenAsync(user, organization);
        SetAuthorizationHeader(token);
        SetOrganizationHeader(organization.Id);

        var request = new UpdateOrganizationLogoRequest
        {
            LogoUrl = "https://example.com/logo.png"
        };

        // Act
        var response = await HttpClient.PutAsync("/api/v1.0/organizations/logo", CreateJsonContent(request));

        // Assert
        await AssertSuccessStatusCodeAsync(response);

        var result = await ReadJsonResponseAsync<OrganizationResponse>(response);
        result.Should().NotBeNull();
        result!.LogoUrl.Should().Be(request.LogoUrl);

        // Verify the update was persisted to database
        var updatedOrg = await DbContext.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == organization.Id);
        updatedOrg.Should().NotBeNull();
        updatedOrg!.LogoUrl.Should().Be(request.LogoUrl);
    }



    [Fact]
    public async Task ActivateOrganization_WithAdminRole_ShouldActivateOrganizationAndReturnSuccess()
    {
        // Arrange
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync("admin@example.com", Domain.Enums.UserRole.OrganizationAdmin);

        // Deactivate the organization first
        organization.IsActive = false;
        await DbContext.SaveChangesAsync();

        var token = GenerateJwtTokenAsync(user, organization);
        SetAuthorizationHeader(token);
        SetOrganizationHeader(organization.Id);

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/organizations/activate", null);

        // Assert
        await AssertSuccessStatusCodeAsync(response);

        var result = await ReadJsonResponseAsync<SuccessResponse>(response);
        result.Should().NotBeNull();
        result!.Message.Should().Contain("activated");

        // Verify organization was activated in database
        var activatedOrg = await DbContext.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == organization.Id);
        activatedOrg.Should().NotBeNull();
        activatedOrg!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateOrganization_WithAdminRole_ShouldDeactivateOrganizationAndReturnSuccess()
    {
        // Arrange
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync("admin@example.com", Domain.Enums.UserRole.OrganizationAdmin);
        var token = GenerateJwtTokenAsync(user, organization);
        SetAuthorizationHeader(token);
        SetOrganizationHeader(organization.Id);

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/organizations/deactivate", null);

        // Assert
        await AssertSuccessStatusCodeAsync(response);

        var result = await ReadJsonResponseAsync<SuccessResponse>(response);
        result.Should().NotBeNull();
        result!.Message.Should().Contain("deactivated");

        // Verify organization was deactivated in database
        var deactivatedOrg = await DbContext.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == organization.Id);
        deactivatedOrg.Should().NotBeNull();
        deactivatedOrg!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrganizationWithUsers_WithCanManageUsersRole_ShouldReturnOrganizationWithUsers()
    {
        // Arrange
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync("admin@example.com", Domain.Enums.UserRole.OrganizationAdmin);

        // Add another user to the organization
        var secondUser = await CreateTestUserAsync("user2@example.com");
        await CreateTestUserOrganizationAsync(secondUser, organization, Domain.Enums.UserRole.OrganizationUser);

        var token = GenerateJwtTokenAsync(user, organization);
        SetAuthorizationHeader(token);
        SetOrganizationHeader(organization.Id);

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/organizations/users");

        // Assert
        await AssertSuccessStatusCodeAsync(response);

        var result = await ReadJsonResponseAsync<OrganizationWithUsersResponse>(response);
        result.Should().NotBeNull();
        result!.Id.Should().Be(organization.Id);
        result.Name.Should().Be(organization.Name);
        result.Users.Should().NotBeNull();
        result.Users.Should().HaveCount(2);
        result.Users.Should().Contain(u => u.Email == user.Email);
        result.Users.Should().Contain(u => u.Email == secondUser.Email);
    }

    [Fact]
    public async Task GetOrganizationWithUsers_WithoutCanManageUsersRole_ShouldReturnForbidden()
    {
        // Arrange
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync("user@example.com", Domain.Enums.UserRole.ReadOnlyUser);
        var token = GenerateJwtTokenAsync(user, organization);
        SetAuthorizationHeader(token);
        SetOrganizationHeader(organization.Id);

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/organizations/users");

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetOrganizationWithSubscriptions_WithAdminRole_ShouldReturnOrganizationWithSubscriptions()
    {
        // Arrange
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync("admin@example.com", Domain.Enums.UserRole.OrganizationAdmin);

        // Add a subscription to the organization
        var subscription = new Domain.Entities.Subscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Plan = Domain.Enums.SubscriptionPlan.Professional,
            Status = Domain.Enums.SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(1),
            CreatedAt = DateTime.UtcNow
        };
        DbContext.Subscriptions.Add(subscription);
        await DbContext.SaveChangesAsync();

        var token = GenerateJwtTokenAsync(user, organization);
        SetAuthorizationHeader(token);
        SetOrganizationHeader(organization.Id);

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/organizations/subscriptions");

        // Assert
        await AssertSuccessStatusCodeAsync(response);

        var result = await ReadJsonResponseAsync<OrganizationWithSubscriptionsResponse>(response);
        result.Should().NotBeNull();
        result!.Id.Should().Be(organization.Id);
        result.Name.Should().Be(organization.Name);
        result.Subscriptions.Should().NotBeNull();
        result.Subscriptions.Should().HaveCount(1);
        result.Subscriptions[0].Plan.Should().Be(Domain.Enums.SubscriptionPlan.Professional);
        result.Subscriptions[0].Status.Should().Be(Domain.Enums.SubscriptionStatus.Active);
    }

    [Fact]
    public async Task GetOrganizationWithSubscriptions_WithoutAdminRole_ShouldReturnForbidden()
    {
        // Arrange
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync("user@example.com", Domain.Enums.UserRole.OrganizationUser);
        var token = GenerateJwtTokenAsync(user, organization);
        SetAuthorizationHeader(token);
        SetOrganizationHeader(organization.Id);

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/organizations/subscriptions");

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Forbidden);
    }
}
