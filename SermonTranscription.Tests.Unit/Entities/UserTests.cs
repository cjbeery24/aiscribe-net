using FluentAssertions;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Tests.Unit.Common;
using Microsoft.EntityFrameworkCore;

namespace SermonTranscription.Tests.Unit.Entities;

/// <summary>
/// Unit tests for User entity business logic
/// </summary>
public class UserTests : BaseUnitTest
{
    [Fact]
    public void User_FullName_ShouldCombineFirstAndLastName()
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        user.FirstName = "John";
        user.LastName = "Doe";

        // Act
        var fullName = user.FullName;

        // Assert
        fullName.Should().Be("John Doe");
    }

    [Fact]
    public void User_MarkEmailAsVerified_ShouldSetVerificationFlags()
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        user.IsEmailVerified = false;
        user.EmailVerificationToken = "test-token";
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(1);

        // Act
        user.MarkEmailAsVerified();

        // Assert
        user.IsEmailVerified.Should().BeTrue();
        user.EmailVerificationToken.Should().BeNull();
        user.EmailVerificationTokenExpiry.Should().BeNull();
    }

    [Fact]
    public void User_UpdateLastLogin_ShouldUpdateTimestamp()
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        var originalLastLogin = user.LastLoginAt;

        // Act
        user.UpdateLastLogin();

        // Assert
        user.LastLoginAt.Should().NotBe(originalLastLogin);
        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void User_CanResetPassword_ShouldCheckTokenValidityAndExpiry(bool isExpired)
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        user.PasswordResetToken = "valid-reset-token";
        user.PasswordResetTokenExpiry = isExpired 
            ? DateTime.UtcNow.AddHours(-1)  // Expired
            : DateTime.UtcNow.AddHours(1);  // Valid

        // Act
        var canReset = user.CanResetPassword();

        // Assert
        canReset.Should().Be(!isExpired);
    }

    [Fact]
    public void User_CanResetPassword_ShouldReturnFalse_WhenTokenIsNull()
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        user.PasswordResetToken = null;

        // Act
        var canReset = user.CanResetPassword();

        // Assert
        canReset.Should().BeFalse();
    }

    [Fact]
    public void User_CanResetPassword_ShouldReturnTrue_WhenTokenIsValidAndNotExpired()
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        user.PasswordResetToken = "valid-token";
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

        // Act
        var canReset = user.CanResetPassword();

        // Assert
        canReset.Should().BeTrue();
    }

    [Fact]
    public void User_ClearPasswordResetToken_ShouldClearTokenAndExpiry()
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        user.PasswordResetToken = "test-token";
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

        // Act
        user.ClearPasswordResetToken();

        // Assert
        user.PasswordResetToken.Should().BeNull();
        user.PasswordResetTokenExpiry.Should().BeNull();
    }

    [Fact]
    public async Task User_CanBeSavedToDatabase_UsingTestDataFactory()
    {
        // Arrange
        var organization = TestDataFactory.OrganizationFaker.Generate();
        var user = TestDataFactory.UserFaker.Generate();
        
        // Create UserOrganization entity with proper navigation properties
        var userOrg = new UserOrganization
        {
            UserId = user.Id,
            OrganizationId = organization.Id,
            Role = UserRole.OrganizationUser,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            User = user,
            Organization = organization
        };
        
        user.UserOrganizations.Add(userOrg);
        organization.UserOrganizations.Add(userOrg);

        // Act
        DbContext.Organizations.Add(organization);
        DbContext.Users.Add(user);
        DbContext.Set<UserOrganization>().Add(userOrg);
        var saveResult = await SaveAndDetachAllAsync();

        // Assert
        saveResult.Should().Be(3); // 1 organization + 1 user + 1 user organization

        // Verify in database
        var savedUser = await DbContext.Users
            .Include(u => u.UserOrganizations)
            .FirstOrDefaultAsync(u => u.Id == user.Id);
        savedUser.Should().NotBeNull();
        savedUser!.Email.Should().Be(user.Email);
        savedUser.UserOrganizations.Should().HaveCount(1);
        savedUser.UserOrganizations.First().OrganizationId.Should().Be(organization.Id);
    }

    [Fact]
    public async Task User_DatabaseConstraints_ShouldEnforceUniqueEmail()
    {
        // Arrange
        var organization = TestDataFactory.OrganizationFaker.Generate();
        var user1 = TestDataFactory.UserFaker.Generate();
        var user2 = TestDataFactory.UserFaker.Generate();
        
        // Set same email for both users
        user1.Email = "duplicate@example.com";
        user2.Email = "duplicate@example.com";
        
        // Create UserOrganization entities for both users
        var userOrg1 = new UserOrganization
        {
            UserId = user1.Id,
            OrganizationId = organization.Id,
            Role = UserRole.OrganizationUser,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            User = user1,
            Organization = organization
        };
        var userOrg2 = new UserOrganization
        {
            UserId = user2.Id,
            OrganizationId = organization.Id,
            Role = UserRole.OrganizationUser,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            User = user2,
            Organization = organization
        };
        
        user1.UserOrganizations.Add(userOrg1);
        user2.UserOrganizations.Add(userOrg2);
        organization.UserOrganizations.Add(userOrg1);
        organization.UserOrganizations.Add(userOrg2);

        DbContext.Organizations.Add(organization);
        DbContext.Users.Add(user1);
        await SaveAndDetachAllAsync();

        // Act & Assert
        DbContext.Users.Add(user2);
        
        // This should throw due to unique email constraint
        var act = async () => await SaveAndDetachAllAsync();
        await act.Should().ThrowAsync<Exception>();
    }
} 