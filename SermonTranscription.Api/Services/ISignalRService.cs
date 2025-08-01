namespace SermonTranscription.Api.Services;

/// <summary>
/// Service for SignalR real-time communication
/// </summary>
public interface ISignalRService
{
    /// <summary>
    /// Send transcription update to all users in a session
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="transcriptionData">The transcription data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendTranscriptionUpdateAsync(string sessionId, object transcriptionData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send session status update to all users in a session
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="status">The session status</param>
    /// <param name="message">Optional status message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendSessionStatusUpdateAsync(string sessionId, string status, string? message = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send notification to all users in an organization
    /// </summary>
    /// <param name="organizationId">The organization ID</param>
    /// <param name="notificationType">Type of notification</param>
    /// <param name="message">Notification message</param>
    /// <param name="data">Optional notification data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendOrganizationNotificationAsync(string organizationId, string notificationType, string message, object? data = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send transcription completion notification
    /// </summary>
    /// <param name="organizationId">The organization ID</param>
    /// <param name="sessionId">The session ID</param>
    /// <param name="transcriptionId">The transcription ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendTranscriptionCompletedAsync(string organizationId, string sessionId, string transcriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send usage limit warning to organization
    /// </summary>
    /// <param name="organizationId">The organization ID</param>
    /// <param name="remainingMinutes">Remaining transcription minutes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendUsageLimitWarningAsync(string organizationId, int remainingMinutes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send usage limit exceeded notification to organization
    /// </summary>
    /// <param name="organizationId">The organization ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendUsageLimitExceededAsync(string organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active connection count for an organization
    /// </summary>
    /// <param name="organizationId">The organization ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of active connections</returns>
    Task<int> GetActiveConnectionCountAsync(string organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnect all users from an organization
    /// </summary>
    /// <param name="organizationId">The organization ID</param>
    /// <param name="reason">Reason for disconnection</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DisconnectOrganizationAsync(string organizationId, string reason, CancellationToken cancellationToken = default);
}
