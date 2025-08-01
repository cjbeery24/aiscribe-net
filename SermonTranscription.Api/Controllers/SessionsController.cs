using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SermonTranscription.Api.Authorization;
using SermonTranscription.Api.Middleware;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Interfaces;

namespace SermonTranscription.Api.Controllers;

[Route("api/v{version:apiVersion}/sessions")]
[ApiVersion("1.0")]
public class SessionsController : BaseAuthenticatedApiController
{
    private readonly ITranscriptionSessionService _sessionService;

    public SessionsController(ITranscriptionSessionService sessionService, ILogger<SessionsController> logger)
        : base(logger)
    {
        _sessionService = sessionService;
    }

    /// <summary>
    /// Create a new transcription session
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TranscriptionSessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateSession(
        [FromBody] CreateTranscriptionSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = HttpContext.GetOrganizationId()!.Value;
        var userId = HttpContext.GetUserId()!.Value;

        var result = await _sessionService.CreateSessionAsync(request, organizationId, userId, cancellationToken);

        return HandleServiceResult(result, () => CreatedAtAction(nameof(GetSession), new { sessionId = result.Data!.Id }, result.Data));
    }

    /// <summary>
    /// Get a transcription session by ID
    /// </summary>
    [HttpGet("{sessionId:guid}")]
    [ProducesResponseType(typeof(TranscriptionSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSession(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = HttpContext.GetOrganizationId()!.Value;

        var result = await _sessionService.GetSessionAsync(sessionId, organizationId, cancellationToken);

        return HandleServiceResult(result, () => Ok(result.Data));
    }

    /// <summary>
    /// Update a transcription session
    /// </summary>
    [HttpPut("{sessionId:guid}")]
    [ProducesResponseType(typeof(TranscriptionSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSession(
        Guid sessionId,
        [FromBody] UpdateTranscriptionSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = HttpContext.GetOrganizationId()!.Value;

        var result = await _sessionService.UpdateSessionAsync(sessionId, request, organizationId, cancellationToken);

        return HandleServiceResult(result, () => Ok(result.Data));
    }

    /// <summary>
    /// Delete a transcription session
    /// </summary>
    [HttpDelete("{sessionId:guid}")]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSession(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = HttpContext.GetOrganizationId()!.Value;

        var result = await _sessionService.DeleteSessionAsync(sessionId, organizationId, cancellationToken);

        return HandleServiceResult(result, () => Ok(new SuccessResponse { Message = "Session deleted successfully" }));
    }

    /// <summary>
    /// Start a transcription session
    /// </summary>
    [HttpPost("{sessionId:guid}/start")]
    [ProducesResponseType(typeof(TranscriptionSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartSession(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = HttpContext.GetOrganizationId()!.Value;

        var result = await _sessionService.StartSessionAsync(sessionId, organizationId, cancellationToken);

        return HandleServiceResult(result, () => Ok(result.Data));
    }

    /// <summary>
    /// Pause a transcription session
    /// </summary>
    [HttpPost("{sessionId:guid}/pause")]
    [ProducesResponseType(typeof(TranscriptionSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PauseSession(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = HttpContext.GetOrganizationId()!.Value;

        var result = await _sessionService.PauseSessionAsync(sessionId, organizationId, cancellationToken);

        return HandleServiceResult(result, () => Ok(result.Data));
    }

    /// <summary>
    /// Resume a transcription session
    /// </summary>
    [HttpPost("{sessionId:guid}/resume")]
    [ProducesResponseType(typeof(TranscriptionSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResumeSession(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = HttpContext.GetOrganizationId()!.Value;

        var result = await _sessionService.ResumeSessionAsync(sessionId, organizationId, cancellationToken);

        return HandleServiceResult(result, () => Ok(result.Data));
    }

    /// <summary>
    /// Complete a transcription session
    /// </summary>
    [HttpPost("{sessionId:guid}/complete")]
    [ProducesResponseType(typeof(TranscriptionSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteSession(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = HttpContext.GetOrganizationId()!.Value;

        var result = await _sessionService.CompleteSessionAsync(sessionId, organizationId, cancellationToken);

        return HandleServiceResult(result, () => Ok(result.Data));
    }

    /// <summary>
    /// Cancel a transcription session
    /// </summary>
    [HttpPost("{sessionId:guid}/cancel")]
    [ProducesResponseType(typeof(TranscriptionSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelSession(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = HttpContext.GetOrganizationId()!.Value;

        var result = await _sessionService.CancelSessionAsync(sessionId, organizationId, cancellationToken);

        return HandleServiceResult(result, () => Ok(result.Data));
    }

    /// <summary>
    /// Search and filter transcription sessions
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(TranscriptionSessionListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SearchSessions(
        [FromQuery] TranscriptionSessionSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = HttpContext.GetOrganizationId()!.Value;

        var result = await _sessionService.SearchSessionsAsync(request, organizationId, cancellationToken);

        return HandleServiceResult(result, () => Ok(result.Data));
    }

    /// <summary>
    /// Get recent transcription sessions
    /// </summary>
    [HttpGet("recent")]
    [ProducesResponseType(typeof(IEnumerable<TranscriptionSessionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRecentSessions(
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        var organizationId = HttpContext.GetOrganizationId()!.Value;

        var result = await _sessionService.GetRecentSessionsAsync(organizationId, count, cancellationToken);

        return HandleServiceResult(result, () => Ok(result.Data));
    }

    /// <summary>
    /// Get active transcription sessions
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IEnumerable<TranscriptionSessionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetActiveSessions(
        CancellationToken cancellationToken = default)
    {
        var organizationId = HttpContext.GetOrganizationId()!.Value;

        var result = await _sessionService.GetActiveSessionsAsync(organizationId, cancellationToken);

        return HandleServiceResult(result, () => Ok(result.Data));
    }

    /// <summary>
    /// Get active session count
    /// </summary>
    [HttpGet("active/count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetActiveSessionCount(
        CancellationToken cancellationToken = default)
    {
        var organizationId = HttpContext.GetOrganizationId()!.Value;

        var result = await _sessionService.GetActiveSessionCountAsync(organizationId, cancellationToken);

        return HandleServiceResult(result, () => Ok(result.Data));
    }

    /// <summary>
    /// Get total session duration
    /// </summary>
    [HttpGet("duration")]
    [ProducesResponseType(typeof(TimeSpan), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTotalSessionDuration(
        [FromQuery] DateTime? fromDate = null,
        CancellationToken cancellationToken = default)
    {
        var organizationId = HttpContext.GetOrganizationId()!.Value;

        var result = await _sessionService.GetTotalSessionDurationAsync(organizationId, fromDate, cancellationToken);

        return HandleServiceResult(result, () => Ok(result.Data));
    }
}
