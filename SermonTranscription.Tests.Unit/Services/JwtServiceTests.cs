using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Infrastructure.Configuration;
using SermonTranscription.Infrastructure.Services;
using SermonTranscription.Tests.Unit.Common;

namespace SermonTranscription.Tests.Unit.Services;

/// <summary>
/// Unit tests for JWT service
/// </summary>
public class JwtServiceTests : BaseUnitTest
{
    private readonly JwtSettings _jwtSettings;
    private readonly Mock<ILogger<JwtService>> _loggerMock;
    private readonly JwtService _jwtService;
    private readonly User _testUser;

    public JwtServiceTests()
    {
        _jwtSettings = new JwtSettings
        {
            SecretKey = "test-secret-key-for-jwt-service-tests-must-be-long-enough-for-hmac-sha256",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7,
            ClockSkewMinutes = 5
        };

        _loggerMock = new Mock<ILogger<JwtService>>();
        _jwtService = new JwtService(Options.Create(_jwtSettings), _loggerMock.Object);

        _testUser = TestDataFactory.UserFaker.Generate();
    }

    [Fact]
    public void GenerateAccessToken_ShouldCreateValidToken()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var role = UserRole.OrganizationAdmin.ToString();

        // Act
        var token = _jwtService.GenerateAccessToken(_testUser, organizationId, role);

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Should().Contain(".");
        token.Split('.').Should().HaveCount(3); // JWT has 3 parts: header.payload.signature
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeUserClaims()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var role = UserRole.OrganizationUser.ToString();

        // Act
        var token = _jwtService.GenerateAccessToken(_testUser, organizationId, role);
        var userInfo = _jwtService.ValidateToken(token);

        // Assert
        userInfo.Should().NotBeNull();
        userInfo!.UserId.Should().Be(_testUser.Id);
        userInfo.Email.Should().Be(_testUser.Email);
        userInfo.Role.Should().Be(role);
        userInfo.OrganizationId.Should().Be(organizationId);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldCreateValidToken()
    {
        // Act
        var refreshToken = _jwtService.GenerateRefreshToken(_testUser);

        // Assert
        refreshToken.Should().NotBeNullOrEmpty();
        refreshToken.Should().NotContain(".");
        refreshToken.Length.Should().BeGreaterThan(50); // Base64 encoded random bytes
    }

    [Fact]
    public void ValidateToken_ShouldReturnUserInfo_ForValidToken()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var role = UserRole.OrganizationAdmin.ToString();
        var token = _jwtService.GenerateAccessToken(_testUser, organizationId, role);

        // Act
        var userInfo = _jwtService.ValidateToken(token);

        // Assert
        userInfo.Should().NotBeNull();
        userInfo!.UserId.Should().Be(_testUser.Id);
        userInfo.Email.Should().Be(_testUser.Email);
        userInfo.Role.Should().Be(role);
        userInfo.OrganizationId.Should().Be(organizationId);
        userInfo.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void ValidateToken_ShouldReturnNull_ForInvalidToken()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var userInfo = _jwtService.ValidateToken(invalidToken);

        // Assert
        userInfo.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_ShouldReturnNull_ForExpiredToken()
    {
        // Arrange
        var expiredSettings = new JwtSettings
        {
            SecretKey = _jwtSettings.SecretKey,
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            ExpirationMinutes = -1, // Expired immediately
            ClockSkewMinutes = 0
        };

        var expiredJwtService = new JwtService(Options.Create(expiredSettings), _loggerMock.Object);
        var organizationId = Guid.NewGuid();
        var role = UserRole.OrganizationUser.ToString();
        var token = expiredJwtService.GenerateAccessToken(_testUser, organizationId, role);

        // Wait a moment to ensure token is expired
        Thread.Sleep(100);

        // Act
        var userInfo = _jwtService.ValidateToken(token);

        // Assert
        userInfo.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_ShouldReturnNull_ForTokenWithWrongIssuer()
    {
        // Arrange
        var wrongIssuerSettings = new JwtSettings
        {
            SecretKey = _jwtSettings.SecretKey,
            Issuer = "WrongIssuer",
            Audience = _jwtSettings.Audience,
            ExpirationMinutes = 60,
            ClockSkewMinutes = 5
        };

        var wrongIssuerJwtService = new JwtService(Options.Create(wrongIssuerSettings), _loggerMock.Object);
        var organizationId = Guid.NewGuid();
        var role = UserRole.OrganizationUser.ToString();
        var token = wrongIssuerJwtService.GenerateAccessToken(_testUser, organizationId, role);

        // Act
        var userInfo = _jwtService.ValidateToken(token);

        // Assert
        userInfo.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_ShouldReturnNull_ForTokenWithWrongAudience()
    {
        // Arrange
        var wrongAudienceSettings = new JwtSettings
        {
            SecretKey = _jwtSettings.SecretKey,
            Issuer = _jwtSettings.Issuer,
            Audience = "WrongAudience",
            ExpirationMinutes = 60,
            ClockSkewMinutes = 5
        };

        var wrongAudienceJwtService = new JwtService(Options.Create(wrongAudienceSettings), _loggerMock.Object);
        var organizationId = Guid.NewGuid();
        var role = UserRole.OrganizationUser.ToString();
        var token = wrongAudienceJwtService.GenerateAccessToken(_testUser, organizationId, role);

        // Act
        var userInfo = _jwtService.ValidateToken(token);

        // Assert
        userInfo.Should().BeNull();
    }

    [Fact]
    public void GetUserIdFromToken_ShouldReturnUserId_ForValidToken()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var role = UserRole.OrganizationUser.ToString();
        var token = _jwtService.GenerateAccessToken(_testUser, organizationId, role);

        // Act
        var userId = _jwtService.GetUserIdFromToken(token);

        // Assert
        userId.Should().Be(_testUser.Id);
    }

    [Fact]
    public void GetUserIdFromToken_ShouldReturnNull_ForInvalidToken()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var userId = _jwtService.GetUserIdFromToken(invalidToken);

        // Assert
        userId.Should().BeNull();
    }

    [Fact]
    public void GetOrganizationIdFromToken_ShouldReturnOrganizationId_ForValidToken()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var role = UserRole.OrganizationAdmin.ToString();
        var token = _jwtService.GenerateAccessToken(_testUser, organizationId, role);

        // Act
        var extractedOrganizationId = _jwtService.GetOrganizationIdFromToken(token);

        // Assert
        extractedOrganizationId.Should().Be(organizationId);
    }

    [Fact]
    public void GetOrganizationIdFromToken_ShouldReturnNull_ForInvalidToken()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var organizationId = _jwtService.GetOrganizationIdFromToken(invalidToken);

        // Assert
        organizationId.Should().BeNull();
    }

    [Theory]
    [InlineData(UserRole.OrganizationAdmin)]
    [InlineData(UserRole.OrganizationUser)]
    [InlineData(UserRole.ReadOnlyUser)]
    public void GenerateAccessToken_ShouldWorkWithAllRoles(UserRole role)
    {
        // Arrange
        var organizationId = Guid.NewGuid();

        // Act
        var token = _jwtService.GenerateAccessToken(_testUser, organizationId, role.ToString());
        var userInfo = _jwtService.ValidateToken(token);

        // Assert
        userInfo.Should().NotBeNull();
        userInfo!.Role.Should().Be(role.ToString());
    }

    [Fact]
    public void GenerateAccessToken_ShouldThrowException_WhenSecretKeyIsTooShort()
    {
        // Arrange
        var shortKeySettings = new JwtSettings
        {
            SecretKey = "short",
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            ExpirationMinutes = 60,
            ClockSkewMinutes = 5
        };

        var shortKeyJwtService = new JwtService(Options.Create(shortKeySettings), _loggerMock.Object);
        var organizationId = Guid.NewGuid();
        var role = UserRole.OrganizationUser.ToString();

        // Act & Assert
        var act = () => shortKeyJwtService.GenerateAccessToken(_testUser, organizationId, role);
        act.Should().Throw<InvalidOperationException>();
    }
} 