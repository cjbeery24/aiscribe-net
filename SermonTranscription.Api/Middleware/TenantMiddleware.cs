using Microsoft.Extensions.Logging;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Interfaces;
using System.Security.Claims;

namespace SermonTranscription.Api.Middleware;

/// <summary>
/// Middleware for multi-tenant context resolution and organization data isolation
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUserRepository userRepository)
    {
        try
        {
            // Skip tenant resolution for public endpoints
            if (IsPublicEndpoint(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // Check if user is authenticated (required for all non-public endpoints)
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                _logger.LogWarning("Unauthenticated request to {Path}", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Authentication required",
                    errors = new[] { "Valid JWT token is required" }
                });
                return;
            }

            // Skip tenant resolution for organization-agnostic endpoints
            if (IsOrganizationAgnosticEndpoint(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // Extract organization context from X-Organization-ID header
            var tenantContext = await ResolveTenantContextAsync(context, userRepository);

            if (tenantContext != null)
            {
                // Store tenant context in HttpContext for downstream middleware and controllers
                context.Items["TenantContext"] = tenantContext;

                // Add organization ID to response headers for debugging/tracking
                context.Response.Headers.Append("X-Organization-ID", tenantContext.OrganizationId.ToString());

                _logger.LogDebug("Tenant context resolved for user {UserId} in organization {OrganizationId}",
                    tenantContext.UserId, tenantContext.OrganizationId);
            }
            else
            {
                // For authenticated requests that require tenant context, return 403 Forbidden
                // This prevents requests from reaching controllers without proper tenant context
                _logger.LogWarning("Failed to resolve tenant context for request to {Path}", context.Request.Path);

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Organization context is required but could not be resolved",
                    errors = new[] { "Missing or invalid X-Organization-ID header", "User is not a member of the specified organization" }
                });
                return; // Don't continue to next middleware
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in tenant middleware for request to {Path}", context.Request.Path);
            throw;
        }
    }

    private async Task<TenantContext?> ResolveTenantContextAsync(HttpContext context, IUserRepository userRepository)
    {
        // Check if user is authenticated
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return null;
        }

        // Extract user ID from claims
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogWarning("User ID claim not found or invalid");
            return null;
        }

        // Extract organization ID from X-Organization-ID header
        var organizationIdHeader = context.Request.Headers["X-Organization-ID"].FirstOrDefault();
        if (string.IsNullOrEmpty(organizationIdHeader) || !Guid.TryParse(organizationIdHeader, out var organizationId))
        {
            _logger.LogWarning("X-Organization-ID header not found or invalid for user {UserId}", userId);
            return null;
        }

        // Get user with organization membership
        var user = await userRepository.GetByIdWithOrganizationsAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", userId);
            return null;
        }

        // Check if user is active
        if (!user.IsActive)
        {
            _logger.LogWarning("Inactive user attempted access: {UserId}", userId);
            return null;
        }

        // Get user's membership in the organization
        var membership = user.GetOrganizationMembership(organizationId);
        if (membership == null)
        {
            _logger.LogWarning("User {UserId} is not a member of organization {OrganizationId}", userId, organizationId);
            return null;
        }

        // Check if user is active in the organization
        if (!membership.IsActive)
        {
            _logger.LogWarning("User {UserId} is not active in organization {OrganizationId}", userId, organizationId);
            return null;
        }

        // Create tenant context
        return new TenantContext
        {
            UserId = userId,
            OrganizationId = organizationId,
            UserRole = membership.Role,
            User = user,
            Organization = membership.Organization,
            Membership = membership
        };
    }

    private static bool IsPublicEndpoint(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;

        // Public endpoints that don't require authentication or tenant context
        return pathValue.StartsWith("/health") ||
               pathValue.StartsWith("/swagger") ||
               pathValue.StartsWith("/api-docs") ||
               pathValue.StartsWith("/auth/login") ||
               pathValue.StartsWith("/auth/register") ||
               pathValue.StartsWith("/auth/refresh") ||
               pathValue.StartsWith("/auth/verify-email") ||
               pathValue.StartsWith("/auth/reset-password") ||
               pathValue.StartsWith("/auth/forgot-password");
    }

    private static bool IsOrganizationAgnosticEndpoint(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;

        // Endpoints that require authentication but don't need organization context
        return pathValue.StartsWith("/auth/organizations"); // Organization discovery endpoint
    }
}

/// <summary>
/// Context information for the current tenant (organization)
/// </summary>
public class TenantContext
{
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public UserRole UserRole { get; set; }
    public User User { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public UserOrganization Membership { get; set; } = null!;

    /// <summary>
    /// Check if the current user is an admin in the organization
    /// </summary>
    public bool IsAdmin => UserRole == UserRole.OrganizationAdmin;

    /// <summary>
    /// Check if the current user can manage users in the organization
    /// </summary>
    public bool CanManageUsers => UserRole == UserRole.OrganizationAdmin;

    /// <summary>
    /// Check if the current user can manage transcriptions in the organization
    /// </summary>
    public bool CanManageTranscriptions => UserRole == UserRole.OrganizationAdmin || UserRole == UserRole.OrganizationUser;

    /// <summary>
    /// Check if the current user can view transcriptions in the organization
    /// </summary>
    public bool CanViewTranscriptions => UserRole == UserRole.OrganizationAdmin ||
                                        UserRole == UserRole.OrganizationUser ||
                                        UserRole == UserRole.ReadOnlyUser;

    /// <summary>
    /// Check if the current user can export transcriptions from the organization
    /// </summary>
    public bool CanExportTranscriptions => UserRole == UserRole.OrganizationAdmin || UserRole == UserRole.OrganizationUser;
}

/// <summary>
/// Extension methods for accessing tenant context
/// </summary>
public static class TenantContextExtensions
{
    /// <summary>
    /// Get the current tenant context from HttpContext
    /// </summary>
    public static TenantContext? GetTenantContext(this HttpContext context)
    {
        return context.Items.TryGetValue("TenantContext", out var tenantContext)
            ? tenantContext as TenantContext
            : null;
    }

    /// <summary>
    /// Get the current organization ID from HttpContext
    /// </summary>
    public static Guid? GetOrganizationId(this HttpContext context)
    {
        return context.GetTenantContext()?.OrganizationId;
    }

    /// <summary>
    /// Get the current user ID from HttpContext
    /// </summary>
    public static Guid? GetUserId(this HttpContext context)
    {
        return context.GetTenantContext()?.UserId;
    }

    /// <summary>
    /// Check if the current user is an admin in the organization
    /// </summary>
    public static bool IsAdmin(this HttpContext context)
    {
        return context.GetTenantContext()?.IsAdmin ?? false;
    }

    /// <summary>
    /// Check if the current user can manage users in the organization
    /// </summary>
    public static bool CanManageUsers(this HttpContext context)
    {
        return context.GetTenantContext()?.CanManageUsers ?? false;
    }

    /// <summary>
    /// Check if the current user can manage transcriptions in the organization
    /// </summary>
    public static bool CanManageTranscriptions(this HttpContext context)
    {
        return context.GetTenantContext()?.CanManageTranscriptions ?? false;
    }

    /// <summary>
    /// Check if the current user can view transcriptions in the organization
    /// </summary>
    public static bool CanViewTranscriptions(this HttpContext context)
    {
        return context.GetTenantContext()?.CanViewTranscriptions ?? false;
    }

    /// <summary>
    /// Check if the current user can export transcriptions from the organization
    /// </summary>
    public static bool CanExportTranscriptions(this HttpContext context)
    {
        return context.GetTenantContext()?.CanExportTranscriptions ?? false;
    }
}
