using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SermonTranscription.Api.Middleware;

namespace SermonTranscription.Api.Controllers;

/// <summary>
/// Base API controller with authorization and user context for authenticated endpoints
/// </summary>
[ApiController]
[Authorize]
public abstract class BaseAuthenticatedApiController : BaseApiController
{
    protected BaseAuthenticatedApiController(ILogger logger) : base(logger)
    {
    }

    /// <summary>
    /// Gets the current user ID from the JWT token claims
    /// </summary>
    /// <returns>User ID</returns>
    protected Guid GetCurrentUserId()
    {
        return HttpContext.GetUserId()!.Value;
    }
}
