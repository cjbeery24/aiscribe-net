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
        _mockUserRepository.Verify(x => x.GetByIdWithOrganizationsAsync(It.IsAny<Guid>(), CancellationToken.None), Times.Never);
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
        _mockUserRepository.Verify(x => x.GetByIdWithOrganizationsAsync(It.IsAny<Guid>(), CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_ShouldResolveTenantContext_ForAuthenticatedRequests()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var (user, membership) = TestDataFactory.CreateUserWithOrganization(organizationId, UserRole.OrganizationAdmin);

        _httpContext.Request.Path = "/api/transcriptions";
        _httpContext.RequestServices = ServiceProvider;
        _httpContext.User = CreateClaimsPrincipal(user.Id, organizationId, UserRole.OrganizationAdmin.ToString());
        _httpContext.Request.Headers["X-Organization-ID"] = organizationId.ToString();

        // Simulate UserContext being set by AuthenticationMiddleware
        var userContext = new UserContext { UserId = user.Id, User = user };
        _httpContext.Items["UserContext"] = userContext;

        _mockUserRepository
            .Setup(x => x.GetByIdWithOrganizationsAsync(user.Id, CancellationToken.None))
            .ReturnsAsync(user);

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockUserRepository.Object);

        // Assert
        _mockUserRepository.Verify(x => x.GetByIdWithOrganizationsAsync(user.Id, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetTenantContext_ForValidUser()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var (user, membership) = TestDataFactory.CreateUserWithOrganization(organizationId, UserRole.OrganizationUser);

        _httpContext.Request.Path = "/api/transcriptions";
        _httpContext.RequestServices = ServiceProvider;
        _httpContext.User = CreateClaimsPrincipal(user.Id, organizationId, UserRole.OrganizationUser.ToString());
        _httpContext.Request.Headers["X-Organization-ID"] = organizationId.ToString();

        // Simulate UserContext being set by AuthenticationMiddleware
        var userContext = new UserContext { UserId = user.Id, User = user };
        _httpContext.Items["UserContext"] = userContext;

        _mockUserRepository
            .Setup(x => x.GetByIdWithOrganizationsAsync(user.Id, CancellationToken.None))
            .ReturnsAsync(user);

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockUserRepository.Object);

        // Assert
        _mockUserRepository.Verify(x => x.GetByIdWithOrganizationsAsync(user.Id, CancellationToken.None), Times.Once);
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
            .Setup(x => x.GetByIdWithOrganizationsAsync(userId, CancellationToken.None))
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
