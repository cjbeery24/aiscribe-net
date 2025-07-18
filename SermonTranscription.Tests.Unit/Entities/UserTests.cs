using FluentAssertions;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Tests.Unit.Common;

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
        user.OrganizationId = organization.Id;

        // Act
        DbContext.Organizations.Add(organization);
        DbContext.Users.Add(user);
        var saveResult = await SaveAndDetachAllAsync();

        // Assert
        saveResult.Should().Be(2); // 1 organization + 1 user

        // Verify in database
        var savedUser = await DbContext.Users.FindAsync(user.Id);
        savedUser.Should().NotBeNull();
        savedUser!.Email.Should().Be(user.Email);
        savedUser.OrganizationId.Should().Be(organization.Id);
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
        user1.OrganizationId = organization.Id;
        user2.OrganizationId = organization.Id;

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