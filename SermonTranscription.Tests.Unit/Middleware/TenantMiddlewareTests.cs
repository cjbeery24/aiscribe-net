using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using SermonTranscription.Api.Middleware;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Interfaces;
using SermonTranscription.Tests.Unit.Common;
using System.Security.Claims;
using Xunit;

namespace SermonTranscription.Tests.Unit.Middleware;

public class TenantMiddlewareTests : BaseUnitTest
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILogger<TenantMiddleware>> _mockLogger;
    private readonly TenantMiddleware _middleware;
    private readonly DefaultHttpContext _httpContext;

    public TenantMiddlewareTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<TenantMiddleware>>();
        _middleware = new TenantMiddleware(next: (context) => Task.CompletedTask, _mockLogger.Object);
        _httpContext = new DefaultHttpContext();
    }

    [Fact]
    public async Task InvokeAsync_ShouldSkipTenantResolution_ForHealthCheckEndpoint()
    {
        // Arrange
        _httpContext.Request.Path = "/health";
        _httpContext.RequestServices = ServiceProvider;

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockUserRepository.Object);

        // Assert
        _mockUserRepository.Verify(x => x.GetByIdWithOrganizationsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("/api/transcriptions")]
    [InlineData("/api/sessions")]
    [InlineData("/api/organizations")]
    public async Task InvokeAsync_ShouldSkipTenantResolution_ForNonAuthenticatedRequests(string path)
    {
        // Arrange
        _httpContext.Request.Path = path;
        _httpContext.RequestServices = ServiceProvider;

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockUserRepository.Object);

        // Assert
        _mockUserRepository.Verify(x => x.GetByIdWithOrganizationsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_ShouldResolveTenantContext_ForAuthenticatedRequests()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var user = TestDataFactory.UserFaker.Generate();
        user.Id = userId;

        _httpContext.Request.Path = "/api/transcriptions";
        _httpContext.RequestServices = ServiceProvider;
        _httpContext.User = CreateClaimsPrincipal(userId, organizationId, UserRole.OrganizationAdmin.ToString());
        _httpContext.Request.Headers["X-Organization-ID"] = organizationId.ToString();

        _mockUserRepository
            .Setup(x => x.GetByIdWithOrganizationsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockUserRepository.Object);

        // Assert
        _mockUserRepository.Verify(x => x.GetByIdWithOrganizationsAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetTenantContext_ForValidUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var user = TestDataFactory.UserFaker.Generate();
        user.Id = userId;

        _httpContext.Request.Path = "/api/transcriptions";
        _httpContext.RequestServices = ServiceProvider;
        _httpContext.User = CreateClaimsPrincipal(userId, organizationId, UserRole.OrganizationUser.ToString());
        _httpContext.Request.Headers["X-Organization-ID"] = organizationId.ToString();

        _mockUserRepository
            .Setup(x => x.GetByIdWithOrganizationsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockUserRepository.Object);

        // Assert
        _mockUserRepository.Verify(x => x.GetByIdWithOrganizationsAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleMissingUser_WithoutThrowingException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        _httpContext.Request.Path = "/api/transcriptions";
        _httpContext.RequestServices = ServiceProvider;
        _httpContext.User = CreateClaimsPrincipal(userId, organizationId, UserRole.OrganizationUser.ToString());
        _httpContext.Request.Headers["X-Organization-ID"] = organizationId.ToString();

        _mockUserRepository
            .Setup(x => x.GetByIdWithOrganizationsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await _middleware.InvokeAsync(_httpContext, _mockUserRepository.Object);
        // Should not throw exception
    }

    private static ClaimsPrincipal CreateClaimsPrincipal(Guid userId, Guid organizationId, string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, "test@example.com"),
            new(ClaimTypes.Name, "Test User")
            // Note: organization_id and role claims are no longer included in JWT tokens
        };

        var identity = new ClaimsIdentity(claims, "Test");
        return new ClaimsPrincipal(identity);
    }
}
