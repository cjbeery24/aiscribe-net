using Microsoft.Extensions.Logging;
using Moq;
using SermonTranscription.Application.Services;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Interfaces;
using SermonTranscription.Domain.Exceptions;
using SermonTranscription.Tests.Unit.Common;
using System.Security.Cryptography;
using Xunit;

namespace SermonTranscription.Tests.Unit.Services;

public class AuthServiceTests : BaseUnitTest
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IUserOrganizationRepository> _mockUserOrganizationRepository;
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IPasswordValidator> _mockPasswordValidator;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockUserOrganizationRepository = new Mock<IUserOrganizationRepository>();
        _mockJwtService = new Mock<IJwtService>();
        _mockLogger = new Mock<ILogger<AuthService>>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockPasswordValidator = new Mock<IPasswordValidator>();

        _authService = new AuthService(
            _mockUserRepository.Object,
            _mockUserOrganizationRepository.Object,
            _mockJwtService.Object,
            _mockLogger.Object,
            _mockPasswordHasher.Object,
            _mockPasswordValidator.Object);
    }

    [Fact]
    public async Task RegisterAsync_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "password123",
            FirstName = "John",
            LastName = "Doe"
        };

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(request.Email, CancellationToken.None))
            .ReturnsAsync((User?)null);

        _mockUserRepository
            .Setup(x => x.AddAsync(It.IsAny<User>(), CancellationToken.None))
            .ReturnsAsync((User user, CancellationToken token) => user);

        _mockPasswordHasher
            .Setup(x => x.HashPassword(request.Password))
            .Returns("hashed_password");

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("User registered successfully. Please check your email to verify your account.", result.Message);
        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<User>(), CancellationToken.None), Times.Once);
        _mockPasswordHasher.Verify(x => x.HashPassword(request.Password), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldReturnFailure()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "password123",
            FirstName = "John",
            LastName = "Doe"
        };

        var existingUser = new User { Email = request.Email };
        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(request.Email, CancellationToken.None))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User with this email already exists", result.Message);
        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<User>(), CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnSuccess()
    {
        // Arrange
        var email = "test@example.com";
        var password = "password123";
        var hashedPassword = "hashed_password";

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = hashedPassword,
            IsActive = true,
            IsEmailVerified = true
        };

        var userOrg = new UserOrganization
        {
            UserId = user.Id,
            OrganizationId = Guid.NewGuid(),
            Role = UserRole.OrganizationAdmin,
            IsActive = true
        };

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(email, CancellationToken.None))
            .ReturnsAsync(user);

        _mockUserOrganizationRepository
            .Setup(x => x.GetUserOrganizationsAsync(user.Id, CancellationToken.None))
            .ReturnsAsync(new List<UserOrganization> { userOrg });

        _mockJwtService
            .Setup(x => x.GenerateAccessToken(user))
            .Returns("access_token");

        _mockJwtService
            .Setup(x => x.GenerateRefreshToken(user))
            .Returns("refresh_token");

        _mockPasswordHasher
            .Setup(x => x.VerifyPassword(password, hashedPassword))
            .Returns(true);

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Login successful", result.Message);
        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RefreshToken);
        Assert.Equal("access_token", result.AccessToken);
        Assert.Equal("refresh_token", result.RefreshToken);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ShouldReturnFailure()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var password = "password123";

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(email, CancellationToken.None))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid email or password", result.Message);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldReturnFailure()
    {
        // Arrange
        var email = "test@example.com";
        var wrongPassword = "wrongpassword123";
        var hashedPassword = "hashed_password";

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = hashedPassword,
            IsActive = true,
            IsEmailVerified = true
        };

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(email, CancellationToken.None))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(x => x.VerifyPassword(wrongPassword, hashedPassword))
            .Returns(false);

        // Act
        var result = await _authService.LoginAsync(email, wrongPassword);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid email or password", result.Message);
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ShouldReturnFailure()
    {
        // Arrange
        var email = "test@example.com";
        var password = "password123";
        var hashedPassword = "hashed_password";

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = hashedPassword,
            IsActive = false
        };

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(email, CancellationToken.None))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Account is deactivated", result.Message);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithValidEmail_ShouldReturnSuccess()
    {
        // Arrange
        var email = "test@example.com";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            IsActive = true
        };

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(email, CancellationToken.None))
            .ReturnsAsync(user);

        _mockUserRepository
            .Setup(x => x.UpdateAsync(It.IsAny<User>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.ForgotPasswordAsync(email);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("If the email address exists in our system, you will receive a password reset link.", result.Message);
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithNonExistentEmail_ShouldReturnSuccess()
    {
        // Arrange
        var email = "nonexistent@example.com";

        _mockUserRepository
            .Setup(x => x.GetByEmailAsync(email, CancellationToken.None))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.ForgotPasswordAsync(email);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("If the email address exists in our system, you will receive a password reset link.", result.Message);
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ShouldReturnSuccess()
    {
        // Arrange
        var token = "valid_reset_token";
        var newPassword = "newpassword123";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordResetToken = token,
            PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1),
            IsActive = true
        };

        _mockUserRepository
            .Setup(x => x.GetByPasswordResetTokenAsync(token, CancellationToken.None))
            .ReturnsAsync(user);

        _mockUserRepository
            .Setup(x => x.UpdateAsync(It.IsAny<User>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        _mockPasswordHasher
            .Setup(x => x.HashPassword(newPassword))
            .Returns("new_hashed_password");

        // Act
        var result = await _authService.ResetPasswordAsync(token, newPassword);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Password has been successfully reset", result.Message);
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), CancellationToken.None), Times.Once);
        _mockPasswordHasher.Verify(x => x.HashPassword(newPassword), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithInvalidToken_ShouldReturnFailure()
    {
        // Arrange
        var token = "invalid_token";
        var newPassword = "newpassword123";

        _mockUserRepository
            .Setup(x => x.GetByPasswordResetTokenAsync(token, CancellationToken.None))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.ResetPasswordAsync(token, newPassword);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid or expired reset token", result.Message);
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithExpiredToken_ShouldReturnFailure()
    {
        // Arrange
        var token = "expired_token";
        var newPassword = "newpassword123";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordResetToken = token,
            PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(-1), // Expired
            IsActive = true
        };

        _mockUserRepository
            .Setup(x => x.GetByPasswordResetTokenAsync(token, CancellationToken.None))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.ResetPasswordAsync(token, newPassword);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Reset token has expired", result.Message);
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithEmptyCredentials_ShouldReturnFailure()
    {
        // Act
        var result = await _authService.LoginAsync("", "password");
        var result2 = await _authService.LoginAsync("email@test.com", "");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Email and password are required", result.Message);
        Assert.False(result2.IsSuccess);
        Assert.Equal("Email and password are required", result2.Message);
    }

    [Fact]
    public async Task RegisterAsync_WithEmptyRequiredFields_ShouldReturnFailure()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "",
            Password = "password123",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("required", result.Message);
    }

    [Fact]
    public async Task RegisterAsync_WithShortPassword_ShouldReturnFailure()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "123",
            FirstName = "John",
            LastName = "Doe"
        };

        _mockPasswordValidator
            .Setup(x => x.Validate(request.Password))
            .Throws(new PasswordValidationDomainException("Password must be at least 8 characters long"));

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Password must be at least 8 characters long", result.Message);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithEmptyEmail_ShouldReturnFailure()
    {
        // Act
        var result = await _authService.ForgotPasswordAsync("");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Email is required", result.Message);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithEmptyToken_ShouldReturnFailure()
    {
        // Act
        var result = await _authService.ResetPasswordAsync("", "newpassword123");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Token and new password are required", result.Message);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithShortPassword_ShouldReturnFailure()
    {
        // Arrange
        var token = "valid_token";
        var shortPassword = "123";

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordResetToken = token,
            PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1)
        };

        _mockUserRepository
            .Setup(x => x.GetByPasswordResetTokenAsync(token, CancellationToken.None))
            .ReturnsAsync(user);

        _mockPasswordValidator
            .Setup(x => x.Validate(shortPassword))
            .Throws(new PasswordValidationDomainException("Password must be at least 8 characters long"));

        // Act
        var result = await _authService.ResetPasswordAsync(token, shortPassword);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Password must be at least 8 characters long", result.Message);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithEmptyToken_ShouldReturnFailure()
    {
        // Act
        var result = await _authService.RefreshTokenAsync("");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Refresh token is required", result.Message);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidToken_ShouldReturnFailure()
    {
        // Arrange
        var invalidToken = "invalid_refresh_token";

        _mockUserRepository
            .Setup(x => x.GetRefreshTokenAsync(invalidToken, CancellationToken.None))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var result = await _authService.RefreshTokenAsync(invalidToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid refresh token", result.Message);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ShouldReturnNewTokens()
    {
        // Arrange
        var refreshToken = "valid_refresh_token";
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            IsActive = true,
            IsEmailVerified = true
        };

        var storedRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            User = user
        };

        var membership = new UserOrganization
        {
            UserId = userId,
            OrganizationId = organizationId,
            Role = UserRole.OrganizationUser,
            IsActive = true
        };

        var newAccessToken = "new_access_token";
        var newRefreshToken = "new_refresh_token";

        _mockUserRepository
            .Setup(x => x.GetRefreshTokenAsync(refreshToken, CancellationToken.None))
            .ReturnsAsync(storedRefreshToken);

        _mockUserOrganizationRepository
            .Setup(x => x.GetUserOrganizationsAsync(userId, CancellationToken.None))
            .ReturnsAsync(new List<UserOrganization> { membership });

        _mockJwtService
            .Setup(x => x.GenerateAccessToken(user))
            .Returns(newAccessToken);

        _mockJwtService
            .Setup(x => x.GenerateRefreshToken(user))
            .Returns(newRefreshToken);

        _mockUserRepository
            .Setup(x => x.RevokeRefreshTokenAsync(refreshToken, CancellationToken.None))
            .Returns(Task.CompletedTask);

        _mockUserRepository
            .Setup(x => x.AddRefreshTokenAsync(It.IsAny<RefreshToken>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        _mockUserRepository
            .Setup(x => x.UpdateAsync(user, CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.RefreshTokenAsync(refreshToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newAccessToken, result.AccessToken);
        Assert.Equal(newRefreshToken, result.RefreshToken);
        Assert.NotNull(result.User);
        Assert.Equal(userId, result.User.UserId);
        // Note: OrganizationId and Role are no longer included in AuthUserInfo
        // as they're determined per-request via X-Organization-ID header

        _mockUserRepository.Verify(x => x.RevokeRefreshTokenAsync(refreshToken, CancellationToken.None), Times.Once);
        _mockUserRepository.Verify(x => x.AddRefreshTokenAsync(It.IsAny<RefreshToken>(), CancellationToken.None), Times.Once);
        _mockUserRepository.Verify(x => x.UpdateAsync(user, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ShouldReturnFailure()
    {
        // Arrange
        var refreshToken = "expired_refresh_token";
        var userId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            IsActive = true
        };

        var storedRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            User = user
        };

        _mockUserRepository
            .Setup(x => x.GetRefreshTokenAsync(refreshToken, CancellationToken.None))
            .ReturnsAsync(storedRefreshToken);

        _mockUserRepository
            .Setup(x => x.RevokeRefreshTokenAsync(refreshToken, CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.RefreshTokenAsync(refreshToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Refresh token has expired", result.Message);

        _mockUserRepository.Verify(x => x.RevokeRefreshTokenAsync(refreshToken, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithRevokedToken_ShouldReturnFailure()
    {
        // Arrange
        var refreshToken = "revoked_refresh_token";
        var userId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            IsActive = true
        };

        var storedRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = DateTime.UtcNow, // Revoked
            User = user
        };

        _mockUserRepository
            .Setup(x => x.GetRefreshTokenAsync(refreshToken, CancellationToken.None))
            .ReturnsAsync(storedRefreshToken);

        // Act
        var result = await _authService.RefreshTokenAsync(refreshToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Refresh token has been revoked", result.Message);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInactiveUser_ShouldReturnFailure()
    {
        // Arrange
        var refreshToken = "valid_refresh_token";
        var userId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            IsActive = false // Inactive user
        };

        var storedRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            User = user
        };

        _mockUserRepository
            .Setup(x => x.GetRefreshTokenAsync(refreshToken, CancellationToken.None))
            .ReturnsAsync(storedRefreshToken);

        _mockUserRepository
            .Setup(x => x.RevokeAllUserRefreshTokensAsync(userId, CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.RefreshTokenAsync(refreshToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User account is deactivated", result.Message);

        _mockUserRepository.Verify(x => x.RevokeAllUserRefreshTokensAsync(userId, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithValidToken_ShouldReturnSuccess()
    {
        // Arrange
        var refreshToken = "valid_refresh_token";

        _mockUserRepository
            .Setup(x => x.RevokeRefreshTokenAsync(refreshToken, CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.RevokeRefreshTokenAsync(refreshToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Refresh token revoked successfully", result.Message);

        _mockUserRepository.Verify(x => x.RevokeRefreshTokenAsync(refreshToken, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithEmptyToken_ShouldReturnFailure()
    {
        // Act
        var result = await _authService.RevokeRefreshTokenAsync("");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Refresh token is required", result.Message);
    }
}
