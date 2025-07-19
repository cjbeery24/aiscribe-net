using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Moq;
using SermonTranscription.Api.Authorization;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Interfaces;
using System.Security.Claims;
using Xunit;

namespace SermonTranscription.Tests.Unit.Authorization;

public class OrganizationAuthorizationHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILogger<OrganizationAuthorizationHandler>> _mockLogger;
    private readonly OrganizationAuthorizationHandler _handler;
    private AuthorizationHandlerContext _context;

    public OrganizationAuthorizationHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<OrganizationAuthorizationHandler>>();
        _handler = new OrganizationAuthorizationHandler(_mockUserRepository.Object, _mockLogger.Object);
        
        // Create a default authorization context
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()), new("organization_id", Guid.NewGuid().ToString()) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        _context = new AuthorizationHandlerContext(
            [new OrganizationRequirement(OrganizationPermissionType.Admin)],
            principal,
            null);
    }

    [Fact]
    public async Task HandleRequirementAsync_WithValidAdminUser_Succeeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        
        var user = CreateTestUser(userId);
        var organization = CreateTestOrganization(organizationId);
        var membership = CreateTestMembership(userId, organizationId, UserRole.OrganizationAdmin);
        
        user.UserOrganizations.Add(membership);
        organization.UserOrganizations.Add(membership);
        
        SetupClaims(userId, organizationId);
        _mockUserRepository.Setup(r => r.GetByIdWithOrganizationsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _handler.HandleAsync(_context);

        // Assert
        Assert.True(_context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_WithValidUserForManageTranscriptions_Succeeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        
        var user = CreateTestUser(userId);
        var organization = CreateTestOrganization(organizationId);
        var membership = CreateTestMembership(userId, organizationId, UserRole.OrganizationUser);
        
        user.UserOrganizations.Add(membership);
        organization.UserOrganizations.Add(membership);
        
        SetupClaims(userId, organizationId);
        _mockUserRepository.Setup(r => r.GetByIdWithOrganizationsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var requirement = new OrganizationRequirement(OrganizationPermissionType.ManageTranscriptions);
        var context = new AuthorizationHandlerContext(
            [requirement],
            _context.User,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_WithReadOnlyUserForViewTranscriptions_Succeeds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        
        var user = CreateTestUser(userId);
        var organization = CreateTestOrganization(organizationId);
        var membership = CreateTestMembership(userId, organizationId, UserRole.ReadOnlyUser);
        
        user.UserOrganizations.Add(membership);
        organization.UserOrganizations.Add(membership);
        
        SetupClaims(userId, organizationId);
        _mockUserRepository.Setup(r => r.GetByIdWithOrganizationsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var requirement = new OrganizationRequirement(OrganizationPermissionType.ViewTranscriptions);
        var context = new AuthorizationHandlerContext(
            [requirement],
            _context.User,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_WithInactiveUser_Fails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        
        var user = CreateTestUser(userId);
        user.IsActive = false;
        
        SetupClaims(userId, organizationId);
        _mockUserRepository.Setup(r => r.GetByIdWithOrganizationsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _handler.HandleAsync(_context);

        // Assert
        Assert.False(_context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_WithInactiveMembership_Fails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        
        var user = CreateTestUser(userId);
        var organization = CreateTestOrganization(organizationId);
        var membership = CreateTestMembership(userId, organizationId, UserRole.OrganizationAdmin);
        membership.IsActive = false;
        
        user.UserOrganizations.Add(membership);
        organization.UserOrganizations.Add(membership);
        
        SetupClaims(userId, organizationId);
        _mockUserRepository.Setup(r => r.GetByIdWithOrganizationsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _handler.HandleAsync(_context);

        // Assert
        Assert.False(_context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_WithNonMemberUser_Fails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        
        var user = CreateTestUser(userId);
        // User has no memberships
        
        SetupClaims(userId, organizationId);
        _mockUserRepository.Setup(r => r.GetByIdWithOrganizationsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _handler.HandleAsync(_context);

        // Assert
        Assert.False(_context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_WithInsufficientRole_Fails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        
        var user = CreateTestUser(userId);
        var organization = CreateTestOrganization(organizationId);
        var membership = CreateTestMembership(userId, organizationId, UserRole.ReadOnlyUser);
        
        user.UserOrganizations.Add(membership);
        organization.UserOrganizations.Add(membership);
        
        SetupClaims(userId, organizationId);
        _mockUserRepository.Setup(r => r.GetByIdWithOrganizationsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act - Try to access admin-only functionality with read-only role
        await _handler.HandleAsync(_context);

        // Assert
        Assert.False(_context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_WithMissingUserIdClaim_Fails()
    {
        // Arrange
        var claims = new List<Claim> { new("organization_id", Guid.NewGuid().ToString()) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        var context = new AuthorizationHandlerContext(
            [new OrganizationRequirement(OrganizationPermissionType.Admin)],
            principal,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_WithMissingOrganizationIdClaim_Fails()
    {
        // Arrange
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        var context = new AuthorizationHandlerContext(
            [new OrganizationRequirement(OrganizationPermissionType.Admin)],
            principal,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_WithUserNotFound_Fails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        
        SetupClaims(userId, organizationId);
        _mockUserRepository.Setup(r => r.GetByIdWithOrganizationsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        await _handler.HandleAsync(_context);

        // Assert
        Assert.False(_context.HasSucceeded);
    }

    [Theory]
    [InlineData(UserRole.OrganizationAdmin, OrganizationPermissionType.Admin, true)]
    [InlineData(UserRole.OrganizationAdmin, OrganizationPermissionType.ManageUsers, true)]
    [InlineData(UserRole.OrganizationAdmin, OrganizationPermissionType.ManageTranscriptions, true)]
    [InlineData(UserRole.OrganizationAdmin, OrganizationPermissionType.ViewTranscriptions, true)]
    [InlineData(UserRole.OrganizationAdmin, OrganizationPermissionType.ExportTranscriptions, true)]
    [InlineData(UserRole.OrganizationUser, OrganizationPermissionType.Admin, false)]
    [InlineData(UserRole.OrganizationUser, OrganizationPermissionType.ManageUsers, false)]
    [InlineData(UserRole.OrganizationUser, OrganizationPermissionType.ManageTranscriptions, true)]
    [InlineData(UserRole.OrganizationUser, OrganizationPermissionType.ViewTranscriptions, true)]
    [InlineData(UserRole.OrganizationUser, OrganizationPermissionType.ExportTranscriptions, true)]
    [InlineData(UserRole.ReadOnlyUser, OrganizationPermissionType.Admin, false)]
    [InlineData(UserRole.ReadOnlyUser, OrganizationPermissionType.ManageUsers, false)]
    [InlineData(UserRole.ReadOnlyUser, OrganizationPermissionType.ManageTranscriptions, false)]
    [InlineData(UserRole.ReadOnlyUser, OrganizationPermissionType.ViewTranscriptions, true)]
    [InlineData(UserRole.ReadOnlyUser, OrganizationPermissionType.ExportTranscriptions, false)]
    public async Task HandleRequirementAsync_WithDifferentRolesAndPermissions_ReturnsExpectedResult(
        UserRole userRole, 
        OrganizationPermissionType requiredPermission, 
        bool expectedResult)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        
        var user = CreateTestUser(userId);
        var organization = CreateTestOrganization(organizationId);
        var membership = CreateTestMembership(userId, organizationId, userRole);
        
        user.UserOrganizations.Add(membership);
        organization.UserOrganizations.Add(membership);
        
        SetupClaims(userId, organizationId);
        _mockUserRepository.Setup(r => r.GetByIdWithOrganizationsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var requirement = new OrganizationRequirement(requiredPermission);
        var context = new AuthorizationHandlerContext(
            [requirement],
            _context.User,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.Equal(expectedResult, context.HasSucceeded);
    }

    private void SetupClaims(Guid userId, Guid organizationId)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()), new("organization_id", organizationId.ToString()) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        // Create a new context with the updated principal
        _context = new AuthorizationHandlerContext(
            [new OrganizationRequirement(OrganizationPermissionType.Admin)],
            principal,
            null);
    }

    private static User CreateTestUser(Guid id)
    {
        return new User
        {
            Id = id,
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Organization CreateTestOrganization(Guid id)
    {
        return new Organization
        {
            Id = id,
            Name = "Test Organization",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static UserOrganization CreateTestMembership(Guid userId, Guid organizationId, UserRole role)
    {
        return new UserOrganization
        {
            UserId = userId,
            OrganizationId = organizationId,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }
} 