using Microsoft.Extensions.Logging;
using SermonTranscription.Api.Authorization;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Interfaces;
using System.Security.Claims;

namespace SermonTranscription.Api.Middleware;

/// <summary>
/// Middleware for user authentication and context resolution
/// </summary>
public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthenticationMiddleware> _logger;

    public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
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

            // Resolve user context for authenticated requests
            var userContext = await ResolveUserContextAsync(context);
            if (userContext == null)
            {
                // User validation failed
                _logger.LogWarning("Failed to resolve user context for request to {Path}", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "User validation failed",
                    errors = new[] { "Invalid or inactive user" }
                });
                return;
            }

            // Store user context for all authenticated requests
            context.Items["UserContext"] = userContext;
            _logger.LogDebug("User context resolved: {UserId}", userContext.UserId);

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in authentication middleware for request to {Path}", context.Request.Path);
            throw;
        }
    }

    private async Task<UserContext?> ResolveUserContextAsync(HttpContext context)
    {
        // Extract user ID from claims
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogWarning("User ID claim not found or invalid");
            return null;
        }

        // Get scoped services from the request scope
        var userCacheService = context.RequestServices.GetRequiredService<IUserCacheService>();

        // Get user from cache or repository
        var user = await userCacheService.GetUserAsync(
            userId,
            CancellationToken.None);

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

        // Create user context
        return new UserContext
        {
            UserId = userId,
            User = user
        };
    }
}

/// <summary>
/// Context information for the current user (for organization-agnostic endpoints)
/// </summary>
public class UserContext
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}

/// <summary>
/// Extension methods for accessing user context information
/// </summary>
public static class UserContextExtensions
{
    /// <summary>
    /// Get the current user context from HttpContext (for organization-agnostic endpoints)
    /// </summary>
    public static UserContext? GetUserContext(this HttpContext context)
    {
        return context.Items.TryGetValue("UserContext", out var userContext)
            ? userContext as UserContext
            : null;
    }

    /// <summary>
    /// Get the current user ID from HttpContext (works for both tenant and user contexts)
    /// </summary>
    public static Guid? GetUserId(this HttpContext context)
    {
        return context.GetUserContext()?.UserId;
    }
}
