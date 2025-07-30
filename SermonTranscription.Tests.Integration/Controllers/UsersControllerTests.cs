using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SermonTranscription.Tests.Integration.Common;
using SermonTranscription.Application.DTOs;
using System.Net;

namespace SermonTranscription.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for user management endpoints
/// </summary>
public class UsersControllerTests : BaseIntegrationTest
{
    public UsersControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetUserProfile_WithValidToken_ShouldReturnUserProfile()
    {
        // Arrange
        var user = await CreateTestUserAsync("test@example.com");
        var token = GenerateJwtTokenAsync(user);
        SetAuthorizationHeader(token);

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/users/profile");

        // Assert
        await AssertSuccessStatusCodeAsync(response);

        var result = await ReadJsonResponseAsync<UserProfileResponse>(response);
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
        result.FirstName.Should().Be(user.FirstName);
        result.LastName.Should().Be(user.LastName);
        result.IsActive.Should().Be(user.IsActive);
    }

    [Fact]
    public async Task GetUserProfile_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearHeaders();

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/users/profile");

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateUserProfile_WithValidData_ShouldUpdateProfileAndReturnSuccess()
    {
        // Arrange
        var user = await CreateTestUserAsync("test@example.com");
        var token = GenerateJwtTokenAsync(user);
        SetAuthorizationHeader(token);

        var request = new UpdateUserProfileRequest
        {
            FirstName = "Updated First",
            LastName = "Updated Last"
        };

        // Act
        var response = await HttpClient.PutAsync("/api/v1.0/users/profile", CreateJsonContent(request));

        // Assert
        await AssertSuccessStatusCodeAsync(response);

        var result = await ReadJsonResponseAsync<UserProfileResponse>(response);
        result.Should().NotBeNull();
        result!.FirstName.Should().Be(request.FirstName);
        result.LastName.Should().Be(request.LastName);
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify the update was persisted to database
        var updatedUser = await DbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.FirstName.Should().Be(request.FirstName);
        updatedUser.LastName.Should().Be(request.LastName);
    }

    [Fact]
    public async Task UpdateUserProfile_WithInvalidData_ShouldReturnValidationErrors()
    {
        // Arrange
        var user = await CreateTestUserAsync("test@example.com");
        var token = GenerateJwtTokenAsync(user);
        SetAuthorizationHeader(token);

        var request = new UpdateUserProfileRequest
        {
            FirstName = "", // Invalid: empty first name
            LastName = new string('A', 101) // Invalid: too long
        };

        // Act
        var response = await HttpClient.PutAsync("/api/v1.0/users/profile", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateUserProfile_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearHeaders();

        var request = new UpdateUserProfileRequest
        {
            FirstName = "Updated First"
        };

        // Act
        var response = await HttpClient.PutAsync("/api/v1.0/users/profile", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_WithValidData_ShouldChangePasswordAndReturnSuccess()
    {
        // Arrange
        var user = await CreateTestUserAsync("test@example.com");
        var token = GenerateJwtTokenAsync(user);
        SetAuthorizationHeader(token);

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "TestPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/users/change-password", CreateJsonContent(request));

        // Assert
        await AssertSuccessStatusCodeAsync(response);

        var result = await ReadSuccessResponseAsync(response);
        result.Should().NotBeNull();
        result!.Message.Should().Be("Password changed successfully");
    }

    [Fact]
    public async Task ChangePassword_WithInvalidCurrentPassword_ShouldReturnUnauthorized()
    {
        // Arrange
        var user = await CreateTestUserAsync("test@example.com");
        var token = GenerateJwtTokenAsync(user);
        SetAuthorizationHeader(token);

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "WrongPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/users/change-password", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_WithMismatchedPasswords_ShouldReturnBadRequest()
    {
        // Arrange
        var user = await CreateTestUserAsync("test@example.com");
        var token = GenerateJwtTokenAsync(user);
        SetAuthorizationHeader(token);

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "TestPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "DifferentPassword123!"
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/users/change-password", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearHeaders();

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "TestPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/users/change-password", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOrganizationUsers_WithCanManageUsersRole_ShouldReturnOrganizationUsers()
    {
        // Arrange
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync("admin@example.com", Domain.Enums.UserRole.OrganizationAdmin);

        // Add another user to the organization
        var secondUser = await CreateTestUserAsync("user2@example.com");
        await CreateTestUserOrganizationAsync(secondUser, organization, Domain.Enums.UserRole.OrganizationUser);

        var token = GenerateJwtTokenAsync(user, organization);
        SetAuthorizationHeader(token);
        SetOrganizationHeader(organization.Id);

        var request = new OrganizationUserSearchRequest
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/users?pageNumber=1&pageSize=10");

        // Assert
        await AssertSuccessStatusCodeAsync(response);

        var result = await ReadJsonResponseAsync<OrganizationUserListResponse>(response);
        result.Should().NotBeNull();
        result!.Users.Should().NotBeNull();
        result.Users.Should().HaveCount(2);
        result.Users.Should().Contain(u => u.Email == user.Email);
        result.Users.Should().Contain(u => u.Email == secondUser.Email);
    }

    [Fact]
    public async Task GetOrganizationUsers_WithoutCanManageUsersRole_ShouldReturnForbidden()
    {
        // Arrange
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync("user@example.com", Domain.Enums.UserRole.ReadOnlyUser);
        var token = GenerateJwtTokenAsync(user, organization);
        SetAuthorizationHeader(token);
        SetOrganizationHeader(organization.Id);

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/users");

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetOrganizationUsers_WithoutOrganizationHeader_ShouldReturnForbidden()
    {
        // Arrange
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync("admin@example.com", Domain.Enums.UserRole.OrganizationAdmin);
        var token = GenerateJwtTokenAsync(user, organization);
        SetAuthorizationHeader(token);
        // Don't set organization header

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/users");

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetOrganizationUser_WithValidUserId_ShouldReturnOrganizationUser()
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
        var response = await HttpClient.GetAsync($"/api/v1.0/users/{secondUser.Id}");

        // Assert
        await AssertSuccessStatusCodeAsync(response);

        var result = await ReadJsonResponseAsync<OrganizationUserResponse>(response);
        result.Should().NotBeNull();
        result!.UserId.Should().Be(secondUser.Id);
        result.Email.Should().Be(secondUser.Email);
        result.Role.Should().Be(Domain.Enums.UserRole.OrganizationUser.ToString());
    }

    [Fact]
    public async Task GetOrganizationUser_WithInvalidUserId_ShouldReturnNotFound()
    {
        // Arrange
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync("admin@example.com", Domain.Enums.UserRole.OrganizationAdmin);
        var token = GenerateJwtTokenAsync(user, organization);
        SetAuthorizationHeader(token);
        SetOrganizationHeader(organization.Id);

        var invalidUserId = Guid.NewGuid();

        // Act
        var response = await HttpClient.GetAsync($"/api/v1.0/users/{invalidUserId}");

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrganizationUser_WithoutCanManageUsersRole_ShouldReturnForbidden()
    {
        // Arrange
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync("user@example.com", Domain.Enums.UserRole.ReadOnlyUser);
        var token = GenerateJwtTokenAsync(user, organization);
        SetAuthorizationHeader(token);
        SetOrganizationHeader(organization.Id);

        // Act
        var response = await HttpClient.GetAsync($"/api/v1.0/users/{user.Id}");

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateOrganizationUserRole_WithValidData_ShouldUpdateRoleAndReturnSuccess()
    {
        // Arrange
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync("admin@example.com", Domain.Enums.UserRole.OrganizationAdmin);

        // Add another user to the organization
        var secondUser = await CreateTestUserAsync("user2@example.com");
        await CreateTestUserOrganizationAsync(secondUser, organization, Domain.Enums.UserRole.OrganizationUser);

        var token = GenerateJwtTokenAsync(user, organization);
        SetAuthorizationHeader(token);
        SetOrganizationHeader(organization.Id);

        var request = new UpdateOrganizationUserRoleRequest
        {
            Role = Domain.Enums.UserRole.OrganizationAdmin.ToString()
        };

        // Act
        var response = await HttpClient.PutAsync($"/api/v1.0/users/{secondUser.Id}/role", CreateJsonContent(request));

        // Assert
        await AssertSuccessStatusCodeAsync(response);

        var result = await ReadJsonResponseAsync<OrganizationUserResponse>(response);
        result.Should().NotBeNull();
        result!.Role.Should().Be(Domain.Enums.UserRole.OrganizationAdmin.ToString());

        // Verify the update was persisted to database
        var updatedUserOrg = await DbContext.UserOrganizations.AsNoTracking()
            .FirstOrDefaultAsync(uo => uo.UserId == secondUser.Id && uo.OrganizationId == organization.Id);
        updatedUserOrg.Should().NotBeNull();
        updatedUserOrg!.Role.Should().Be(Domain.Enums.UserRole.OrganizationAdmin);
    }

    [Fact]
    public async Task UpdateOrganizationUserRole_WithInvalidUserId_ShouldReturnNotFound()
    {
        // Arrange
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync("admin@example.com", Domain.Enums.UserRole.OrganizationAdmin);
        var token = GenerateJwtTokenAsync(user, organization);
        SetAuthorizationHeader(token);
        SetOrganizationHeader(organization.Id);

        var invalidUserId = Guid.NewGuid();
        var request = new UpdateOrganizationUserRoleRequest
        {
            Role = Domain.Enums.UserRole.OrganizationAdmin.ToString()
        };

        // Act
        var response = await HttpClient.PutAsync($"/api/v1.0/users/{invalidUserId}/role", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateOrganizationUserRole_WithoutCanManageUsersRole_ShouldReturnForbidden()
    {
        // Arrange
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync("user@example.com", Domain.Enums.UserRole.OrganizationUser);
        var token = GenerateJwtTokenAsync(user, organization);
        SetAuthorizationHeader(token);
        SetOrganizationHeader(organization.Id);

        var request = new UpdateOrganizationUserRoleRequest
        {
            Role = Domain.Enums.UserRole.OrganizationAdmin.ToString()
        };

        // Act
        var response = await HttpClient.PutAsync($"/api/v1.0/users/{user.Id}/role", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemoveUserFromOrganization_WithValidUserId_ShouldRemoveUserAndReturnSuccess()
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
        var response = await HttpClient.DeleteAsync($"/api/v1/users/{secondUser.Id}");

        // Assert
        await AssertSuccessStatusCodeAsync(response);

        var result = await ReadSuccessResponseAsync(response);
        result.Should().NotBeNull();
        result!.Message.Should().Contain("removed");

        // Verify the user was removed from organization in database
        var userOrg = await DbContext.UserOrganizations.AsNoTracking()
            .FirstOrDefaultAsync(uo => uo.UserId == secondUser.Id && uo.OrganizationId == organization.Id);
        userOrg.Should().BeNull();
    }

    [Fact]
    public async Task RemoveUserFromOrganization_WithInvalidUserId_ShouldReturnNotFound()
    {
        // Arrange
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync("admin@example.com", Domain.Enums.UserRole.OrganizationAdmin);
        var token = GenerateJwtTokenAsync(user, organization);
        SetAuthorizationHeader(token);
        SetOrganizationHeader(organization.Id);

        var invalidUserId = Guid.NewGuid();

        // Act
        var response = await HttpClient.DeleteAsync($"/api/v1.0/users/{invalidUserId}");

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveUserFromOrganization_WithoutCanManageUsersRole_ShouldReturnForbidden()
    {
        // Arrange
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync("user@example.com", Domain.Enums.UserRole.OrganizationUser);
        var token = GenerateJwtTokenAsync(user, organization);
        SetAuthorizationHeader(token);
        SetOrganizationHeader(organization.Id);

        // Act
        var response = await HttpClient.DeleteAsync($"/api/v1.0/users/{user.Id}");

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeactivateOrganizationUser_WithValidUserId_ShouldDeactivateUserAndReturnSuccess()
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
        var response = await HttpClient.PostAsync($"/api/v1.0/users/{secondUser.Id}/deactivate", null);

        // Assert
        await AssertSuccessStatusCodeAsync(response);

        var result = await ReadSuccessResponseAsync(response);
        result.Should().NotBeNull();
        result!.Message.Should().Contain("deactivated");

        // Verify the user was deactivated in organization in database
        var userOrg = await DbContext.UserOrganizations.AsNoTracking()
            .FirstOrDefaultAsync(uo => uo.UserId == secondUser.Id && uo.OrganizationId == organization.Id);
        userOrg.Should().NotBeNull();
        userOrg!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateOrganizationUser_WithInvalidUserId_ShouldReturnNotFound()
    {
        // Arrange
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync("admin@example.com", Domain.Enums.UserRole.OrganizationAdmin);
        var token = GenerateJwtTokenAsync(user, organization);
        SetAuthorizationHeader(token);
        SetOrganizationHeader(organization.Id);

        var invalidUserId = Guid.NewGuid();

        // Act
        var response = await HttpClient.PostAsync($"/api/v1.0/users/{invalidUserId}/deactivate", null);

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeactivateOrganizationUser_WithoutCanManageUsersRole_ShouldReturnForbidden()
    {
        // Arrange
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync("user@example.com", Domain.Enums.UserRole.OrganizationUser);
        var token = GenerateJwtTokenAsync(user, organization);
        SetAuthorizationHeader(token);
        SetOrganizationHeader(organization.Id);

        // Act
        var response = await HttpClient.PostAsync($"/api/v1.0/users/{user.Id}/deactivate", null);

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ActivateOrganizationUser_WithValidUserId_ShouldActivateUserAndReturnSuccess()
    {
        // Arrange
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync("admin@example.com", Domain.Enums.UserRole.OrganizationAdmin);

        // Add another user to the organization and deactivate them
        var secondUser = await CreateTestUserAsync("user2@example.com");
        var userOrg = await CreateTestUserOrganizationAsync(secondUser, organization, Domain.Enums.UserRole.OrganizationUser);
        userOrg.IsActive = false;
        await DbContext.SaveChangesAsync();

        var token = GenerateJwtTokenAsync(user, organization);
        SetAuthorizationHeader(token);
        SetOrganizationHeader(organization.Id);

        // Act
        var response = await HttpClient.PostAsync($"/api/v1.0/users/{secondUser.Id}/activate", null);

        // Assert
        await AssertSuccessStatusCodeAsync(response);

        var result = await ReadSuccessResponseAsync(response);
        result.Should().NotBeNull();
        result!.Message.Should().Contain("activated");

        // Verify the user was activated in organization in database
        var updatedUserOrg = await DbContext.UserOrganizations.AsNoTracking()
            .FirstOrDefaultAsync(uo => uo.UserId == secondUser.Id && uo.OrganizationId == organization.Id);
        updatedUserOrg.Should().NotBeNull();
        updatedUserOrg!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ActivateOrganizationUser_WithInvalidUserId_ShouldReturnNotFound()
    {
        // Arrange
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync("admin@example.com", Domain.Enums.UserRole.OrganizationAdmin);
        var token = GenerateJwtTokenAsync(user, organization);
        SetAuthorizationHeader(token);
        SetOrganizationHeader(organization.Id);

        var invalidUserId = Guid.NewGuid();

        // Act
        var response = await HttpClient.PostAsync($"/api/v1.0/users/{invalidUserId}/activate", null);

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ActivateOrganizationUser_WithoutCanManageUsersRole_ShouldReturnForbidden()
    {
        // Arrange
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync("user@example.com", Domain.Enums.UserRole.OrganizationUser);
        var token = GenerateJwtTokenAsync(user, organization);
        SetAuthorizationHeader(token);
        SetOrganizationHeader(organization.Id);

        // Act
        var response = await HttpClient.PostAsync($"/api/v1.0/users/{user.Id}/activate", null);

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Forbidden);
    }
}
