using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SermonTranscription.Api.Hubs;

namespace SermonTranscription.Api.Services;

/// <summary>
/// Service for SignalR real-time communication
/// </summary>
public class SignalRService : ISignalRService
{
    private readonly IHubContext<TranscriptionHub> _hubContext;
    private readonly ILogger<SignalRService> _logger;

    public SignalRService(IHubContext<TranscriptionHub> hubContext, ILogger<SignalRService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Send transcription update to all users in a session
    /// </summary>
    public async Task SendTranscriptionUpdateAsync(string sessionId, object transcriptionData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Sending transcription update for session {SessionId}", sessionId);

            await _hubContext.Clients.Group($"session_{sessionId}").SendAsync("TranscriptionUpdate", new
            {
                SessionId = sessionId,
                Data = transcriptionData,
                Timestamp = DateTime.UtcNow
            }, cancellationToken);

            _logger.LogDebug("Transcription update sent successfully for session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending transcription update for session {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Send session status update to all users in a session
    /// </summary>
    public async Task SendSessionStatusUpdateAsync(string sessionId, string status, string? message = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending session status update: {Status} for session {SessionId}", status, sessionId);

            await _hubContext.Clients.Group($"session_{sessionId}").SendAsync("SessionStatusUpdate", new
            {
                SessionId = sessionId,
                Status = status,
                Message = message,
                Timestamp = DateTime.UtcNow
            }, cancellationToken);

            _logger.LogDebug("Session status update sent successfully for session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending session status update for session {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Send notification to all users in an organization
    /// </summary>
    public async Task SendOrganizationNotificationAsync(string organizationId, string notificationType, string message, object? data = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending organization notification: {Type} - {Message} to organization {OrganizationId}",
                notificationType, message, organizationId);

            await _hubContext.Clients.Group($"org_{organizationId}").SendAsync("OrganizationNotification", new
            {
                Type = notificationType,
                Message = message,
                Data = data,
                Timestamp = DateTime.UtcNow
            }, cancellationToken);

            _logger.LogDebug("Organization notification sent successfully to organization {OrganizationId}", organizationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending organization notification to organization {OrganizationId}", organizationId);
        }
    }

    /// <summary>
    /// Send transcription completion notification
    /// </summary>
    public async Task SendTranscriptionCompletedAsync(string organizationId, string sessionId, string transcriptionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending transcription completion notification for session {SessionId}", sessionId);

            // Send to session participants
            await _hubContext.Clients.Group($"session_{sessionId}").SendAsync("TranscriptionCompleted", new
            {
                SessionId = sessionId,
                TranscriptionId = transcriptionId,
                Timestamp = DateTime.UtcNow
            }, cancellationToken);

            // Send to organization (for admins and other users who might be interested)
            await _hubContext.Clients.Group($"org_{organizationId}").SendAsync("TranscriptionCompleted", new
            {
                SessionId = sessionId,
                TranscriptionId = transcriptionId,
                Timestamp = DateTime.UtcNow
            }, cancellationToken);

            _logger.LogDebug("Transcription completion notification sent successfully for session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending transcription completion notification for session {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Send usage limit warning to organization
    /// </summary>
    public async Task SendUsageLimitWarningAsync(string organizationId, int remainingMinutes, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogWarning("Sending usage limit warning to organization {OrganizationId}. Remaining minutes: {RemainingMinutes}",
                organizationId, remainingMinutes);

            await _hubContext.Clients.Group($"org_{organizationId}").SendAsync("UsageLimitWarning", new
            {
                RemainingMinutes = remainingMinutes,
                Message = $"You have {remainingMinutes} transcription minutes remaining this month.",
                Timestamp = DateTime.UtcNow
            }, cancellationToken);

            _logger.LogDebug("Usage limit warning sent successfully to organization {OrganizationId}", organizationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending usage limit warning to organization {OrganizationId}", organizationId);
        }
    }

    /// <summary>
    /// Send usage limit exceeded notification to organization
    /// </summary>
    public async Task SendUsageLimitExceededAsync(string organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogWarning("Sending usage limit exceeded notification to organization {OrganizationId}", organizationId);

            await _hubContext.Clients.Group($"org_{organizationId}").SendAsync("UsageLimitExceeded", new
            {
                Message = "You have exceeded your transcription minutes limit for this month. Please upgrade your subscription to continue.",
                Timestamp = DateTime.UtcNow
            }, cancellationToken);

            _logger.LogDebug("Usage limit exceeded notification sent successfully to organization {OrganizationId}", organizationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending usage limit exceeded notification to organization {OrganizationId}", organizationId);
        }
    }

    /// <summary>
    /// Get active connection count for an organization
    /// </summary>
    public async Task<int> GetActiveConnectionCountAsync(string organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            // This is a simplified implementation
            // In a production environment, you might want to use a more sophisticated approach
            // like Redis or a database to track connections across multiple server instances

            _logger.LogDebug("Getting active connection count for organization {OrganizationId}", organizationId);

            // For now, we'll return 0 as the actual implementation would require
            // accessing the hub's connection tracking from outside the hub
            // This could be implemented with a shared connection store (Redis, database, etc.)
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active connection count for organization {OrganizationId}", organizationId);
            return 0;
        }
    }

    /// <summary>
    /// Disconnect all users from an organization
    /// </summary>
    public async Task DisconnectOrganizationAsync(string organizationId, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogWarning("Disconnecting all users from organization {OrganizationId}. Reason: {Reason}",
                organizationId, reason);

            await _hubContext.Clients.Group($"org_{organizationId}").SendAsync("Disconnected", new
            {
                Reason = reason,
                Message = $"You have been disconnected: {reason}",
                Timestamp = DateTime.UtcNow
            }, cancellationToken);

            _logger.LogDebug("Disconnect notification sent successfully to organization {OrganizationId}", organizationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting users from organization {OrganizationId}", organizationId);
        }
    }
}
