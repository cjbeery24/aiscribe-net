using FluentAssertions;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Exceptions;
using SermonTranscription.Tests.Unit.Common;
using Xunit;

namespace SermonTranscription.Tests.Unit.Entities;

public class UserOrganizationTests : BaseUnitTest
{
    [Theory]
    [InlineData(UserRole.OrganizationAdmin, true)]
    [InlineData(UserRole.OrganizationUser, false)]
    [InlineData(UserRole.ReadOnlyUser, false)]
    public void IsAdmin_ShouldReturnCorrectValue(UserRole role, bool expected)
    {
        // Arrange
        var userOrganization = TestDataFactory.UserOrganizationFaker.Generate();
        userOrganization.Role = role;

        // Act
        var result = userOrganization.IsAdmin();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(UserRole.OrganizationAdmin, true)]
    [InlineData(UserRole.OrganizationUser, false)]
    [InlineData(UserRole.ReadOnlyUser, false)]
    public void CanManageUsers_ShouldReturnCorrectValue(UserRole role, bool expected)
    {
        // Arrange
        var userOrganization = TestDataFactory.UserOrganizationFaker.Generate();
        userOrganization.Role = role;

        // Act
        var result = userOrganization.CanManageUsers();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(UserRole.OrganizationAdmin, true)]
    [InlineData(UserRole.OrganizationUser, true)]
    [InlineData(UserRole.ReadOnlyUser, false)]
    public void CanManageTranscriptions_ShouldReturnCorrectValue(UserRole role, bool expected)
    {
        // Arrange
        var userOrganization = TestDataFactory.UserOrganizationFaker.Generate();
        userOrganization.Role = role;

        // Act
        var result = userOrganization.CanManageTranscriptions();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(UserRole.OrganizationAdmin, true, true)]
    [InlineData(UserRole.OrganizationUser, true, true)]
    [InlineData(UserRole.ReadOnlyUser, true, true)]
    [InlineData(UserRole.OrganizationAdmin, false, false)]
    [InlineData(UserRole.OrganizationUser, false, false)]
    [InlineData(UserRole.ReadOnlyUser, false, false)]
    public void CanViewTranscriptions_ShouldReturnCorrectValue(UserRole role, bool isActive, bool expected)
    {
        // Arrange
        var userOrganization = TestDataFactory.UserOrganizationFaker.Generate();
        userOrganization.Role = role;
        userOrganization.IsActive = isActive;

        // Act
        var result = userOrganization.CanViewTranscriptions();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(UserRole.OrganizationAdmin, true)]
    [InlineData(UserRole.OrganizationUser, true)]
    [InlineData(UserRole.ReadOnlyUser, false)]
    public void CanExportTranscriptions_ShouldReturnCorrectValue(UserRole role, bool expected)
    {
        // Arrange
        var userOrganization = TestDataFactory.UserOrganizationFaker.Generate();
        userOrganization.Role = role;

        // Act
        var result = userOrganization.CanExportTranscriptions();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void UpdateRole_ShouldUpdateRoleAndTimestamp()
    {
        // Arrange
        var userOrganization = TestDataFactory.UserOrganizationFaker.Generate();
        userOrganization.Role = UserRole.OrganizationUser;
        var originalUpdateTime = userOrganization.UpdatedAt;
        var newRole = UserRole.OrganizationAdmin;

        // Act
        userOrganization.UpdateRole(newRole);

        // Assert
        userOrganization.Role.Should().Be(newRole);
        userOrganization.UpdatedAt.Should().BeAfter(originalUpdateTime ?? DateTime.MinValue);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var userOrganization = TestDataFactory.UserOrganizationFaker.Generate();
        userOrganization.IsActive = true;
        var originalUpdateTime = userOrganization.UpdatedAt;

        // Act
        userOrganization.Deactivate();

        // Assert
        userOrganization.IsActive.Should().BeFalse();
        userOrganization.UpdatedAt.Should().BeAfter(originalUpdateTime ?? DateTime.MinValue);
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var userOrganization = TestDataFactory.UserOrganizationFaker.Generate();
        userOrganization.IsActive = false;
        var originalUpdateTime = userOrganization.UpdatedAt;

        // Act
        userOrganization.Activate();

        // Assert
        userOrganization.IsActive.Should().BeTrue();
        userOrganization.UpdatedAt.Should().BeAfter(originalUpdateTime ?? DateTime.MinValue);
    }

    [Fact]
    public void AcceptInvitation_ShouldSetInvitationAcceptedAndClearToken()
    {
        // Arrange
        var userOrganization = TestDataFactory.UserOrganizationFaker.Generate();
        userOrganization.InvitationToken = "test-token";
        userOrganization.InvitationAcceptedAt = null;
        userOrganization.IsActive = false;
        var originalUpdateTime = userOrganization.UpdatedAt;

        // Act
        userOrganization.AcceptInvitation();

        // Assert
        userOrganization.InvitationAcceptedAt.Should().NotBeNull();
        userOrganization.InvitationToken.Should().BeNull();
        userOrganization.IsActive.Should().BeTrue();
        userOrganization.UpdatedAt.Should().BeAfter(originalUpdateTime ?? DateTime.MinValue);
    }

    [Fact]
    public void ValidateCanManageUsers_ShouldNotThrowException_WhenUserIsAdmin()
    {
        // Arrange
        var userOrganization = TestDataFactory.UserOrganizationFaker.Generate();
        userOrganization.Role = UserRole.OrganizationAdmin;

        // Act & Assert
        userOrganization.Invoking(uo => uo.ValidateCanManageUsers())
            .Should().NotThrow();
    }

    [Fact]
    public void ValidateCanManageUsers_ShouldThrowException_WhenUserIsNotAdmin()
    {
        // Arrange
        var userOrganization = TestDataFactory.UserOrganizationFaker.Generate();
        userOrganization.Role = UserRole.OrganizationUser;

        // Act & Assert
        userOrganization.Invoking(uo => uo.ValidateCanManageUsers())
            .Should().Throw<UserPermissionException>()
            .WithMessage("*does not have permission to manage users*");
    }

    [Fact]
    public void ValidateCanManageTranscriptions_ShouldNotThrowException_WhenUserCanManage()
    {
        // Arrange
        var userOrganization = TestDataFactory.UserOrganizationFaker.Generate();
        userOrganization.Role = UserRole.OrganizationUser;

        // Act & Assert
        userOrganization.Invoking(uo => uo.ValidateCanManageTranscriptions())
            .Should().NotThrow();
    }

    [Fact]
    public void ValidateCanManageTranscriptions_ShouldThrowException_WhenUserCannotManage()
    {
        // Arrange
        var userOrganization = TestDataFactory.UserOrganizationFaker.Generate();
        userOrganization.Role = UserRole.ReadOnlyUser;

        // Act & Assert
        userOrganization.Invoking(uo => uo.ValidateCanManageTranscriptions())
            .Should().Throw<UserPermissionException>()
            .WithMessage("*does not have permission to manage transcriptions*");
    }

    [Fact]
    public void ValidateCanViewTranscriptions_ShouldNotThrowException_WhenUserCanView()
    {
        // Arrange
        var userOrganization = TestDataFactory.UserOrganizationFaker.Generate();
        userOrganization.Role = UserRole.ReadOnlyUser;
        userOrganization.IsActive = true;

        // Act & Assert
        userOrganization.Invoking(uo => uo.ValidateCanViewTranscriptions())
            .Should().NotThrow();
    }

    [Fact]
    public void ValidateCanViewTranscriptions_ShouldThrowException_WhenUserIsInactive()
    {
        // Arrange
        var userOrganization = TestDataFactory.UserOrganizationFaker.Generate();
        userOrganization.Role = UserRole.ReadOnlyUser;
        userOrganization.IsActive = false;

        // Act & Assert
        userOrganization.Invoking(uo => uo.ValidateCanViewTranscriptions())
            .Should().Throw<UserPermissionException>()
            .WithMessage("*does not have permission to view transcriptions*");
    }

    [Fact]
    public void ValidateIsActive_ShouldNotThrowException_WhenUserIsActive()
    {
        // Arrange
        var userOrganization = TestDataFactory.UserOrganizationFaker.Generate();
        userOrganization.IsActive = true;

        // Act & Assert
        userOrganization.Invoking(uo => uo.ValidateIsActive())
            .Should().NotThrow();
    }

    [Fact]
    public void ValidateIsActive_ShouldThrowException_WhenUserIsInactive()
    {
        // Arrange
        var userOrganization = TestDataFactory.UserOrganizationFaker.Generate();
        userOrganization.IsActive = false;

        // Act & Assert
        userOrganization.Invoking(uo => uo.ValidateIsActive())
            .Should().Throw<UserAuthenticationException>()
            .WithMessage("*is not active in organization*");
    }
} 