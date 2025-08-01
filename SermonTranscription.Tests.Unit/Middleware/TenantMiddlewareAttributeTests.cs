using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using SermonTranscription.Api.Authorization;
using SermonTranscription.Api.Middleware;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Interfaces;
using SermonTranscription.Tests.Unit.Common;
using System.Security.Claims;
using Xunit;

namespace SermonTranscription.Tests.Unit.Middleware;

public class TenantMiddlewareAttributeTests : BaseUnitTest
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IUserOrganizationCacheService> _mockUserOrganizationCacheService;
    private readonly Mock<ILogger<TenantMiddleware>> _mockLogger;
    private readonly TenantMiddleware _middleware;
    private readonly DefaultHttpContext _httpContext;

    public TenantMiddlewareAttributeTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockUserOrganizationCacheService = new Mock<IUserOrganizationCacheService>();
        _mockLogger = new Mock<ILogger<TenantMiddleware>>();

        _middleware = new TenantMiddleware(
            next: (context) => Task.CompletedTask,
            _mockLogger.Object,
            _mockUserOrganizationCacheService.Object);
        _httpContext = new DefaultHttpContext();
    }

    [Fact]
    public async Task InvokeAsync_ShouldSkipTenantResolution_ForPublicEndpointAttribute()
    {
        // Arrange
        _httpContext.Request.Path = "/api/v1.0/auth/login";
        _httpContext.Request.Method = "POST";
        _httpContext.RequestServices = ServiceProvider;

        // Create endpoint with PublicEndpoint attribute
        var endpoint = CreateEndpointWithAttribute<PublicEndpointAttribute>();
        _httpContext.SetEndpoint(endpoint);

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockUserRepository.Object);

        // Assert
        _mockUserRepository.Verify(x => x.GetByIdWithOrganizationsAsync(It.IsAny<Guid>(), CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_ShouldSkipTenantResolution_ForOrganizationAgnosticAttribute()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = TestDataFactory.UserFaker.Generate();
        user.Id = userId;

        _httpContext.Request.Path = "/api/v1.0/auth/organizations";
        _httpContext.Request.Method = "GET";
        _httpContext.RequestServices = ServiceProvider;
        _httpContext.User = CreateClaimsPrincipal(userId);

        // Create endpoint with OrganizationAgnostic attribute
        var endpoint = CreateEndpointWithAttribute<OrganizationAgnosticAttribute>();
        _httpContext.SetEndpoint(endpoint);

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockUserRepository.Object);

        // Assert
        _mockUserRepository.Verify(x => x.GetByIdWithOrganizationsAsync(It.IsAny<Guid>(), CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_ShouldRequireTenantContext_ForRegularEndpoint()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var (user, membership) = TestDataFactory.CreateUserWithOrganization(organizationId, UserRole.OrganizationUser);

        _httpContext.Request.Path = "/api/v1.0/transcriptions";
        _httpContext.Request.Method = "GET";
        _httpContext.RequestServices = ServiceProvider;
        _httpContext.User = CreateClaimsPrincipal(user.Id);
        _httpContext.Request.Headers["X-Organization-ID"] = organizationId.ToString();

        // Create endpoint without any special attributes
        var endpoint = CreateEndpointWithoutAttributes();
        _httpContext.SetEndpoint(endpoint);

        // Simulate UserContext being set by AuthenticationMiddleware
        var userContext = new UserContext { UserId = user.Id, User = user };
        _httpContext.Items["UserContext"] = userContext;

        _mockUserOrganizationCacheService
            .Setup(x => x.GetUserWithOrganizationsAsync(user.Id, It.IsAny<Func<Guid, CancellationToken, Task<User?>>>(), CancellationToken.None))
            .ReturnsAsync(user);

        // Act
        await _middleware.InvokeAsync(_httpContext, _mockUserRepository.Object);

        // Assert
        _mockUserOrganizationCacheService.Verify(x => x.GetUserWithOrganizationsAsync(user.Id, It.IsAny<Func<Guid, CancellationToken, Task<User?>>>(), CancellationToken.None), Times.Once);
    }

    private static Endpoint CreateEndpointWithAttribute<T>() where T : Attribute, new()
    {
        var metadata = new EndpointMetadataCollection(new T());
        return new Endpoint(
            requestDelegate: (context) => Task.CompletedTask,
            metadata: metadata,
            displayName: "Test Endpoint"
        );
    }

    private static Endpoint CreateEndpointWithoutAttributes()
    {
        var metadata = new EndpointMetadataCollection();
        return new Endpoint(
            requestDelegate: (context) => Task.CompletedTask,
            metadata: metadata,
            displayName: "Test Endpoint"
        );
    }

    private static ClaimsPrincipal CreateClaimsPrincipal(Guid userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, "test@example.com"),
            new(ClaimTypes.Name, "Test User")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        return new ClaimsPrincipal(identity);
    }
}
