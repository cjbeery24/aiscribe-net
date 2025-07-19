using FluentAssertions;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Exceptions;
using SermonTranscription.Tests.Unit.Common;

namespace SermonTranscription.Tests.Unit.Entities;

/// <summary>
/// Unit tests for enhanced User entity with role-based permissions and validation
/// </summary>
public class UserEnhancedTests : BaseUnitTest
{
    private readonly Organization _testOrganization;
    private readonly User _testUser;

    public UserEnhancedTests()
    {
        _testOrganization = TestDataFactory.OrganizationFaker.Generate();
        _testUser = TestDataFactory.UserFaker.Generate();
    }

    [Fact]
    public void User_DefaultRole_ShouldBeOrganizationUser()
    {
        // Arrange & Act
        var user = TestDataFactory.UserFaker.Generate();
        var organization = TestDataFactory.OrganizationFaker.Generate();
        user.JoinOrganization(organization.Id, UserRole.OrganizationUser);
        
        // Assert
        user.GetOrganizationMembership(organization.Id)!.Role.Should().Be(UserRole.OrganizationUser);
    }

    [Theory]
    [InlineData(UserRole.OrganizationAdmin, true)]
    [InlineData(UserRole.OrganizationUser, false)]
    [InlineData(UserRole.ReadOnlyUser, false)]
    public void User_IsAdmin_ShouldReturnCorrectValue(UserRole role, bool expected)
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        var organization = TestDataFactory.OrganizationFaker.Generate();
        user.JoinOrganization(organization.Id, role);

        // Act
        var isAdmin = user.IsAdmin(organization.Id);

        // Assert
        isAdmin.Should().Be(expected);
    }

    [Theory]
    [InlineData(UserRole.OrganizationAdmin, true)]
    [InlineData(UserRole.OrganizationUser, false)]
    [InlineData(UserRole.ReadOnlyUser, false)]
    public void User_CanManageUsers_ShouldReturnCorrectValue(UserRole role, bool expected)
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        var organization = TestDataFactory.OrganizationFaker.Generate();
        user.JoinOrganization(organization.Id, role);

        // Act
        var canManage = user.CanManageUsers(organization.Id);

        // Assert
        canManage.Should().Be(expected);
    }

    [Theory]
    [InlineData(UserRole.OrganizationAdmin, true)]
    [InlineData(UserRole.OrganizationUser, true)]
    [InlineData(UserRole.ReadOnlyUser, false)]
    public void User_CanManageTranscriptions_ShouldReturnCorrectValue(UserRole role, bool expected)
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        var organization = TestDataFactory.OrganizationFaker.Generate();
        user.JoinOrganization(organization.Id, role);

        // Act
        var canManage = user.CanManageTranscriptions(organization.Id);

        // Assert
        canManage.Should().Be(expected);
    }

    [Theory]
    [InlineData(UserRole.OrganizationAdmin, true, true)]
    [InlineData(UserRole.OrganizationUser, true, true)]
    [InlineData(UserRole.ReadOnlyUser, true, true)]
    [InlineData(UserRole.OrganizationAdmin, false, false)]
    [InlineData(UserRole.OrganizationUser, false, false)]
    [InlineData(UserRole.ReadOnlyUser, false, false)]
    public void User_CanViewTranscriptions_ShouldReturnCorrectValue(UserRole role, bool isActive, bool expected)
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        var organization = TestDataFactory.OrganizationFaker.Generate();
        user.JoinOrganization(organization.Id, role);
        user.IsActive = isActive;

        // Act
        var canView = user.CanViewTranscriptions(organization.Id);

        // Assert
        canView.Should().Be(expected);
    }

    [Theory]
    [InlineData(UserRole.OrganizationAdmin, true)]
    [InlineData(UserRole.OrganizationUser, true)]
    [InlineData(UserRole.ReadOnlyUser, false)]
    public void User_CanExportTranscriptions_ShouldReturnCorrectValue(UserRole role, bool expected)
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        var organization = TestDataFactory.OrganizationFaker.Generate();
        user.JoinOrganization(organization.Id, role);

        // Act
        var canExport = user.CanExportTranscriptions(organization.Id);

        // Assert
        canExport.Should().Be(expected);
    }

    [Fact]
    public void User_UpdateRole_ShouldUpdateRoleAndTimestamp()
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        var organization = TestDataFactory.OrganizationFaker.Generate();
        user.JoinOrganization(organization.Id, UserRole.OrganizationUser);
        var membership = user.GetOrganizationMembership(organization.Id)!;
        var originalRole = membership.Role;
        var originalUpdatedAt = membership.UpdatedAt;

        // Act
        user.UpdateRoleInOrganization(organization.Id, UserRole.OrganizationAdmin);

        // Assert
        user.GetOrganizationMembership(organization.Id)!.Role.Should().Be(UserRole.OrganizationAdmin);
        user.GetOrganizationMembership(organization.Id)!.Role.Should().NotBe(originalRole);
        membership.UpdatedAt.Should().BeAfter(originalUpdatedAt ?? DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void User_Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        user.IsActive = true;

        // Act
        user.Deactivate();

        // Assert
        user.IsActive.Should().BeFalse();
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void User_Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        user.IsActive = false;

        // Act
        user.Activate();

        // Assert
        user.IsActive.Should().BeTrue();
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void User_IsEmailVerificationExpired_ShouldReturnTrue_WhenTokenIsExpired()
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        user.EmailVerificationToken = "test-token";
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(-1);

        // Act
        var isExpired = user.IsEmailVerificationExpired();

        // Assert
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void User_IsEmailVerificationExpired_ShouldReturnFalse_WhenTokenIsValid()
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        user.EmailVerificationToken = "test-token";
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(1);

        // Act
        var isExpired = user.IsEmailVerificationExpired();

        // Assert
        isExpired.Should().BeFalse();
    }

    [Fact]
    public void User_GenerateEmailVerificationToken_ShouldCreateTokenAndExpiry()
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;

        // Act
        user.GenerateEmailVerificationToken();

        // Assert
        user.EmailVerificationToken.Should().NotBeNullOrEmpty();
        user.EmailVerificationTokenExpiry.Should().BeAfter(DateTime.UtcNow);
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void User_GeneratePasswordResetToken_ShouldCreateTokenAndExpiry()
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;

        // Act
        user.GeneratePasswordResetToken();

        // Assert
        user.PasswordResetToken.Should().NotBeNullOrEmpty();
        user.PasswordResetTokenExpiry.Should().BeAfter(DateTime.UtcNow);
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void User_ValidateCanManageUsers_ShouldThrowException_WhenUserIsNotAdmin()
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        var organization = TestDataFactory.OrganizationFaker.Generate();
        user.JoinOrganization(organization.Id, UserRole.OrganizationUser);

        // Act & Assert
        var act = () => user.ValidateCanManageUsers(organization.Id);
        act.Should().Throw<UserPermissionException>()
            .WithMessage("*manage users*");
    }

    [Fact]
    public void User_ValidateCanManageUsers_ShouldNotThrowException_WhenUserIsAdmin()
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        var organization = TestDataFactory.OrganizationFaker.Generate();
        user.JoinOrganization(organization.Id, UserRole.OrganizationAdmin);

        // Act & Assert
        var act = () => user.ValidateCanManageUsers(organization.Id);
        act.Should().NotThrow();
    }

    [Fact]
    public void User_ValidateCanManageTranscriptions_ShouldThrowException_WhenUserCannotManage()
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        var organization = TestDataFactory.OrganizationFaker.Generate();
        user.JoinOrganization(organization.Id, UserRole.ReadOnlyUser);

        // Act & Assert
        var act = () => user.ValidateCanManageTranscriptions(organization.Id);
        act.Should().Throw<UserPermissionException>()
            .WithMessage("*manage transcriptions*");
    }

    [Fact]
    public void User_ValidateCanViewTranscriptions_ShouldThrowException_WhenUserIsInactive()
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        var organization = TestDataFactory.OrganizationFaker.Generate();
        user.JoinOrganization(organization.Id, UserRole.OrganizationUser);
        user.IsActive = false;

        // Act & Assert
        var act = () => user.ValidateCanViewTranscriptions(organization.Id);
        act.Should().Throw<UserPermissionException>()
            .WithMessage("*view transcriptions*");
    }

    [Fact]
    public void User_ValidateEmailVerification_ShouldThrowException_WhenEmailNotVerified()
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        user.IsEmailVerified = false;

        // Act & Assert
        var act = () => user.ValidateEmailVerification();
        act.Should().Throw<UserEmailVerificationException>()
            .WithMessage("*email is not verified*");
    }

    [Fact]
    public void User_ValidateIsActive_ShouldThrowException_WhenUserIsInactive()
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        user.IsActive = false;

        // Act & Assert
        var act = () => user.ValidateIsActive();
        act.Should().Throw<UserAuthenticationException>()
            .WithMessage("*active*");
    }

    [Fact]
    public void User_ValidatePasswordResetToken_ShouldThrowException_WhenTokenIsInvalid()
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        user.PasswordResetToken = null;

        // Act & Assert
        var act = () => user.ValidatePasswordResetToken();
        act.Should().Throw<UserPasswordResetException>()
            .WithMessage("*password reset token*");
    }

    [Fact]
    public void User_ValidatePasswordResetToken_ShouldThrowException_WhenTokenIsExpired()
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        user.PasswordResetToken = "test-token";
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(-1);

        // Act & Assert
        var act = () => user.ValidatePasswordResetToken();
        act.Should().Throw<UserPasswordResetException>()
            .WithMessage("*expired*");
    }

    [Fact]
    public void User_ValidatePasswordResetToken_ShouldNotThrowException_WhenTokenIsValid()
    {
        // Arrange
        var user = TestDataFactory.UserFaker.Generate();
        user.PasswordResetToken = "test-token";
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

        // Act & Assert
        var act = () => user.ValidatePasswordResetToken();
        act.Should().NotThrow();
    }
} 