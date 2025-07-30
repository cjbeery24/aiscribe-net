using Microsoft.Extensions.Logging;
using Moq;
using SermonTranscription.Application.Common;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Services;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Exceptions;
using SermonTranscription.Domain.Interfaces;
using Xunit;

namespace SermonTranscription.Tests.Unit.Services;

public class InvitationServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IUserOrganizationRepository> _mockUserOrganizationRepository;
    private readonly Mock<IOrganizationRepository> _mockOrganizationRepository;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly Mock<ILogger<InvitationService>> _mockLogger;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IPasswordValidator> _mockPasswordValidator;
    private readonly InvitationService _invitationService;

    public InvitationServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockUserOrganizationRepository = new Mock<IUserOrganizationRepository>();
        _mockOrganizationRepository = new Mock<IOrganizationRepository>();
        _mockEmailService = new Mock<IEmailService>();
        _mockJwtService = new Mock<IJwtService>();
        _mockLogger = new Mock<ILogger<InvitationService>>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockPasswordValidator = new Mock<IPasswordValidator>();

        _invitationService = new InvitationService(
            _mockUserRepository.Object,
            _mockUserOrganizationRepository.Object,
            _mockOrganizationRepository.Object,
            _mockEmailService.Object,
            _mockJwtService.Object,
            _mockLogger.Object,
            _mockPasswordHasher.Object,
            _mockPasswordValidator.Object);
    }

    #region InviteUserAsync Tests

    [Fact]
    public async Task InviteUserAsync_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var request = new InviteUserRequest
        {
            Email = "newuser@example.com",
            FirstName = "John",
            LastName = "Doe",
            Role = "OrganizationUser",
            Message = "Welcome to our organization!"
        };

        var organizationId = Guid.NewGuid();
        var invitedByUserId = Guid.NewGuid();
        var invitingUser = new User
        {
            Id = invitedByUserId,
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@example.com"
        };

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(request.Email, CancellationToken.None))
            .ReturnsAsync((User?)null);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(invitedByUserId, CancellationToken.None))
            .ReturnsAsync(invitingUser);

        _mockUserRepository
            .Setup(x => x.AddAsync(It.IsAny<User>(), CancellationToken.None))
            .ReturnsAsync((User user, CancellationToken token) => user);

        _mockUserOrganizationRepository
            .Setup(x => x.AddAsync(It.IsAny<UserOrganization>(), CancellationToken.None))
            .ReturnsAsync((UserOrganization userOrganization, CancellationToken token) => userOrganization);

        _mockEmailService
            .Setup(x => x.SendInvitationEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .ReturnsAsync(true);

        _mockOrganizationRepository
            .Setup(x => x.GetByIdAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(new Organization { Name = "Test Organization" });

        // Act
        var result = await _invitationService.InviteUserAsync(request, organizationId, invitedByUserId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(request.Email, result.Data.Email);

        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<User>(), CancellationToken.None), Times.Once);
        _mockUserOrganizationRepository.Verify(x => x.AddAsync(It.IsAny<UserOrganization>(), CancellationToken.None), Times.Once);
        _mockEmailService.Verify(x => x.SendInvitationEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task InviteUserAsync_WithExistingUser_ShouldReturnSuccess()
    {
        // Arrange
        var request = new InviteUserRequest
        {
            Email = "existinguser@example.com",
            FirstName = "John",
            LastName = "Doe",
            Role = "OrganizationUser"
        };

        var organizationId = Guid.NewGuid();
        var invitedByUserId = Guid.NewGuid();
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            FirstName = "John",
            LastName = "Doe"
        };

        var invitingUser = new User
        {
            Id = invitedByUserId,
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@example.com"
        };

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(request.Email, CancellationToken.None))
            .ReturnsAsync(existingUser);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(invitedByUserId, CancellationToken.None))
            .ReturnsAsync(invitingUser);

        _mockUserOrganizationRepository
            .Setup(x => x.GetUserOrganizationAsync(existingUser.Id, organizationId, CancellationToken.None))
            .ReturnsAsync((UserOrganization?)null);

        _mockUserRepository
            .Setup(x => x.UpdateAsync(It.IsAny<User>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        _mockUserOrganizationRepository
            .Setup(x => x.AddAsync(It.IsAny<UserOrganization>(), CancellationToken.None))
            .ReturnsAsync((UserOrganization userOrganization, CancellationToken token) => userOrganization);

        _mockEmailService
            .Setup(x => x.SendInvitationEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .ReturnsAsync(true);

        _mockOrganizationRepository
            .Setup(x => x.GetByIdAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(new Organization { Name = "Test Organization" });

        // Act
        var result = await _invitationService.InviteUserAsync(request, organizationId, invitedByUserId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(request.Email, result.Data.Email);

        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), CancellationToken.None), Times.Never);
        _mockUserOrganizationRepository.Verify(x => x.AddAsync(It.IsAny<UserOrganization>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task InviteUserAsync_WithExistingMembership_ShouldReturnFailure()
    {
        // Arrange
        var request = new InviteUserRequest
        {
            Email = "existingmember@example.com",
            FirstName = "John",
            LastName = "Doe",
            Role = "OrganizationUser"
        };

        var organizationId = Guid.NewGuid();
        var invitedByUserId = Guid.NewGuid();
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email
        };

        var existingMembership = new UserOrganization
        {
            UserId = existingUser.Id,
            OrganizationId = organizationId,
            Role = UserRole.OrganizationUser
        };

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(request.Email, CancellationToken.None))
            .ReturnsAsync(existingUser);

        _mockUserOrganizationRepository
            .Setup(x => x.GetUserOrganizationAsync(existingUser.Id, organizationId, CancellationToken.None))
            .ReturnsAsync(existingMembership);

        _mockOrganizationRepository
            .Setup(x => x.GetByIdAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(new Organization { Name = "Test Organization" });

        // Act
        var result = await _invitationService.InviteUserAsync(request, organizationId, invitedByUserId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.ErrorCode == ErrorCode.Conflict);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task InviteUserAsync_WithInvalidEmail_ShouldErrorValidationError(string email)
    {
        // Arrange
        var request = new InviteUserRequest
        {
            Email = email,
            FirstName = "John",
            LastName = "Doe",
            Role = "OrganizationUser"
        };

        var organizationId = Guid.NewGuid();
        var invitedByUserId = Guid.NewGuid();

        // Act & Assert
        var result = await _invitationService.InviteUserAsync(request, organizationId, invitedByUserId);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.ErrorCode == ErrorCode.ValidationError);
    }

    [Theory]
    [InlineData("", "Doe")]
    [InlineData("John", "")]
    [InlineData(null, "Doe")]
    [InlineData("John", null)]
    [InlineData("   ", "Doe")]
    [InlineData("John", "   ")]
    public async Task InviteUserAsync_WithInvalidName_ShouldErrorValidationError(string firstName, string lastName)
    {
        // Arrange
        var request = new InviteUserRequest
        {
            Email = "test@example.com",
            FirstName = firstName,
            LastName = lastName,
            Role = "OrganizationUser"
        };

        var organizationId = Guid.NewGuid();
        var invitedByUserId = Guid.NewGuid();

        // Act & Assert
        var result = await _invitationService.InviteUserAsync(request, organizationId, invitedByUserId);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.ErrorCode == ErrorCode.ValidationError);
    }

    [Fact]
    public async Task InviteUserAsync_WithInvalidInvitingUser_ShouldErrorNotFound()
    {
        // Arrange
        var request = new InviteUserRequest
        {
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            Role = "OrganizationUser"
        };

        var organizationId = Guid.NewGuid();
        var invitedByUserId = Guid.NewGuid();

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(request.Email, CancellationToken.None))
            .ReturnsAsync((User?)null);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(invitedByUserId, CancellationToken.None))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var result = await _invitationService.InviteUserAsync(request, organizationId, invitedByUserId);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.ErrorCode == ErrorCode.NotFound);
    }

    #endregion

    #region AcceptInvitationAsync Tests

    [Fact]
    public async Task AcceptInvitationAsync_WithValidToken_ShouldReturnSuccess()
    {
        // Arrange
        var request = new AcceptInvitationRequest
        {
            InvitationToken = "valid-token-123",
            Password = "newpassword123"
        };

        var userOrganization = new UserOrganization
        {
            UserId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Role = UserRole.OrganizationUser,
            InvitationToken = request.InvitationToken,
            InvitationAcceptedAt = null,
            CreatedAt = DateTime.UtcNow,
            Organization = new Organization { Name = "Test Organization" }
        };

        var user = new User
        {
            Id = userOrganization.UserId,
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };

        _mockUserOrganizationRepository
            .Setup(x => x.GetByInvitationTokenAsync(request.InvitationToken, CancellationToken.None))
            .ReturnsAsync(userOrganization);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userOrganization.UserId, CancellationToken.None))
            .ReturnsAsync(user);

        _mockUserRepository
            .Setup(x => x.UpdateAsync(It.IsAny<User>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        _mockUserOrganizationRepository
            .Setup(x => x.UpdateAsync(It.IsAny<UserOrganization>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        _mockPasswordHasher
            .Setup(x => x.HashPassword(request.Password))
            .Returns("hashed_password");

        _mockJwtService
            .Setup(x => x.GenerateAccessToken(user))
            .Returns("access_token");

        _mockJwtService
            .Setup(x => x.GenerateRefreshToken(user))
            .Returns("refresh_token");

        _mockEmailService
            .Setup(x => x.SendWelcomeEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _invitationService.AcceptInvitationAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Test Organization", result.Data.OrganizationName);
        Assert.Equal("OrganizationUser", result.Data.Role);

        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), CancellationToken.None), Times.Once);
        _mockUserOrganizationRepository.Verify(x => x.UpdateAsync(It.IsAny<UserOrganization>(), CancellationToken.None), Times.Once);
        _mockPasswordHasher.Verify(x => x.HashPassword(request.Password), Times.Once);
        _mockEmailService.Verify(x => x.SendWelcomeEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithInvalidToken_ShouldReturnFailure()
    {
        // Arrange
        var request = new AcceptInvitationRequest
        {
            InvitationToken = "invalid-token",
            Password = "newpassword123"
        };

        _mockUserOrganizationRepository
            .Setup(x => x.GetByInvitationTokenAsync(request.InvitationToken, CancellationToken.None))
            .ReturnsAsync((UserOrganization?)null);

        // Act
        var result = await _invitationService.AcceptInvitationAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.ErrorCode == ErrorCode.NotFound);
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithAlreadyAcceptedToken_ShouldReturnFailure()
    {
        // Arrange
        var request = new AcceptInvitationRequest
        {
            InvitationToken = "already-accepted-token",
            Password = "newpassword123"
        };

        var userOrganization = new UserOrganization
        {
            UserId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Role = UserRole.OrganizationUser,
            InvitationToken = request.InvitationToken,
            InvitationAcceptedAt = DateTime.UtcNow.AddDays(-1) // Already accepted
        };

        _mockUserOrganizationRepository
            .Setup(x => x.GetByInvitationTokenAsync(request.InvitationToken, CancellationToken.None))
            .ReturnsAsync(userOrganization);

        // Act
        var result = await _invitationService.AcceptInvitationAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.ErrorCode == ErrorCode.Unauthorized);
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithExpiredToken_ShouldReturnFailure()
    {
        // Arrange
        var request = new AcceptInvitationRequest
        {
            InvitationToken = "expired-token",
            Password = "newpassword123"
        };

        var userOrganization = new UserOrganization
        {
            UserId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Role = UserRole.OrganizationUser,
            InvitationToken = request.InvitationToken,
            InvitationAcceptedAt = null,
            CreatedAt = DateTime.UtcNow.AddDays(-8) // Expired (older than 7 days)
        };

        _mockUserOrganizationRepository
            .Setup(x => x.GetByInvitationTokenAsync(request.InvitationToken, CancellationToken.None))
            .ReturnsAsync(userOrganization);

        // Act
        var result = await _invitationService.AcceptInvitationAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.ErrorCode == ErrorCode.Unauthorized);
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithInvalidPassword_ShouldErrorValidationError()
    {
        // Arrange
        var request = new AcceptInvitationRequest
        {
            InvitationToken = "valid-token",
            Password = "123" // Too short
        };

        var userOrganization = new UserOrganization
        {
            UserId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Role = UserRole.OrganizationUser,
            InvitationToken = request.InvitationToken,
            InvitationAcceptedAt = null,
            CreatedAt = DateTime.UtcNow
        };

        _mockUserOrganizationRepository
            .Setup(x => x.GetByInvitationTokenAsync(request.InvitationToken, CancellationToken.None))
            .ReturnsAsync(userOrganization);

        _mockPasswordValidator
            .Setup(x => x.Validate(request.Password))
            .Throws(new PasswordValidationDomainException("Password must be at least 8 characters long"));

        // Act & Assert
        var result = await _invitationService.AcceptInvitationAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.ErrorCode == ErrorCode.ValidationError);
    }

    [Fact]
    public async Task AcceptInvitationAsync_WithUserNotFound_ShouldErrorNotFound()
    {
        // Arrange
        var request = new AcceptInvitationRequest
        {
            InvitationToken = "valid-token",
            Password = "newpassword123"
        };

        var userOrganization = new UserOrganization
        {
            UserId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Role = UserRole.OrganizationUser,
            InvitationToken = request.InvitationToken,
            InvitationAcceptedAt = null,
            CreatedAt = DateTime.UtcNow
        };

        _mockUserOrganizationRepository
            .Setup(x => x.GetByInvitationTokenAsync(request.InvitationToken, CancellationToken.None))
            .ReturnsAsync(userOrganization);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userOrganization.UserId, CancellationToken.None))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var result = await _invitationService.AcceptInvitationAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.ErrorCode == ErrorCode.NotFound);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task AcceptInvitationAsync_WithEmptyToken_ShouldErrorValidationError(string token)
    {
        // Arrange
        var request = new AcceptInvitationRequest
        {
            InvitationToken = token,
            Password = "newpassword123"
        };

        // Act & Assert
        var result = await _invitationService.AcceptInvitationAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.ErrorCode == ErrorCode.ValidationError);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task AcceptInvitationAsync_WithEmptyPassword_ShouldErrorValidationError(string password)
    {
        // Arrange
        var request = new AcceptInvitationRequest
        {
            InvitationToken = "valid-token",
            Password = password
        };

        _mockPasswordValidator
            .Setup(x => x.Validate(password))
            .Throws(new PasswordValidationDomainException("Password is required"));

        // Act & Assert
        var result = await _invitationService.AcceptInvitationAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.ErrorCode == ErrorCode.ValidationError);
    }

    #endregion

    #region Helper Method Tests

    // Note: Helper methods are private and cannot be tested directly.
    // They are tested indirectly through the public methods that use them.

    #endregion
}
