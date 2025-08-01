using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SermonTranscription.Api.Authorization;
using SermonTranscription.Api.Middleware;
using SermonTranscription.Domain.Enums;
using System.Security.Claims;

namespace SermonTranscription.Api.Hubs;

/// <summary>
/// SignalR hub for real-time transcription communication
/// Handles live transcription updates, session management, and user notifications
/// </summary>
public class TranscriptionHub : Hub
{
    private readonly ILogger<TranscriptionHub> _logger;
    private static readonly Dictionary<string, ConnectionInfo> _activeConnections = new();
    private static readonly object _connectionsLock = new();

    public TranscriptionHub(ILogger<TranscriptionHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Connection information for tracking active connections
    /// </summary>
    private class ConnectionInfo
    {
        public string ConnectionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string OrganizationId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; }
        public UserRole UserRole { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        try
        {
            var user = Context.User;
            var isAuthenticated = user?.Identity?.IsAuthenticated == true;

            string userId;
            string organizationId;
            UserRole userRole = UserRole.Anonymous;

            if (isAuthenticated)
            {
                // Authenticated user
                userId = user!.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
                organizationId = Context.GetHttpContext()?.Request.Headers["X-Organization-ID"].FirstOrDefault() ?? string.Empty;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(organizationId))
                {
                    _logger.LogWarning("Authenticated user missing user ID or organization ID for connection {ConnectionId}", Context.ConnectionId);
                    Context.Abort();
                    return;
                }

                // Get user role from claims
                var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
                userRole = Enum.TryParse<UserRole>(roleClaim, out var role) ? role : UserRole.OrganizationUser;
            }
            else
            {
                // Anonymous user
                userId = $"anon_{Guid.NewGuid():N}";
                organizationId = Context.GetHttpContext()?.Request.Headers["X-Organization-ID"].FirstOrDefault() ?? string.Empty;

                if (string.IsNullOrEmpty(organizationId))
                {
                    _logger.LogWarning("Anonymous user missing organization ID for connection {ConnectionId}", Context.ConnectionId);
                    Context.Abort();
                    return;
                }
            }

            // Add connection to tracking
            var connectionInfo = new ConnectionInfo
            {
                ConnectionId = Context.ConnectionId,
                UserId = userId,
                OrganizationId = organizationId,
                ConnectedAt = DateTime.UtcNow,
                UserRole = userRole
            };

            lock (_connectionsLock)
            {
                _activeConnections[Context.ConnectionId] = connectionInfo;
            }

            // Add to organization group for broadcasting
            await Groups.AddToGroupAsync(Context.ConnectionId, $"org_{organizationId}");

            _logger.LogInformation("User {UserId} connected to transcription hub for organization {OrganizationId} (Authenticated: {IsAuthenticated})",
                userId, organizationId, isAuthenticated);

            // Send connection confirmation
            await Clients.Caller.SendAsync("Connected", new
            {
                ConnectionId = Context.ConnectionId,
                UserId = userId,
                OrganizationId = organizationId,
                ConnectedAt = connectionInfo.ConnectedAt,
                IsAuthenticated = isAuthenticated,
                UserRole = userRole.ToString()
            });

            await base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during connection for {ConnectionId}", Context.ConnectionId);
            Context.Abort();
        }
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            lock (_connectionsLock)
            {
                if (_activeConnections.TryGetValue(Context.ConnectionId, out var connectionInfo))
                {
                    connectionInfo.IsActive = false;
                    _activeConnections.Remove(Context.ConnectionId);

                    _logger.LogInformation("User {UserId} disconnected from transcription hub", connectionInfo.UserId);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disconnection for {ConnectionId}", Context.ConnectionId);
        }
    }

    /// <summary>
    /// Join a transcription session
    /// </summary>
    /// <param name="sessionId">The transcription session ID to join</param>
    public async Task JoinSession(string sessionId)
    {
        try
        {
            var connectionInfo = GetConnectionInfo();
            if (connectionInfo == null) return;

            // Add to session group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"session_{sessionId}");

            // Update connection info
            lock (_connectionsLock)
            {
                if (_activeConnections.TryGetValue(Context.ConnectionId, out var info))
                {
                    info.SessionId = sessionId;
                }
            }

            _logger.LogInformation("User {UserId} joined session {SessionId}", connectionInfo.UserId, sessionId);

            // Notify other users in the session
            await Clients.Group($"session_{sessionId}").SendAsync("UserJoinedSession", new
            {
                UserId = connectionInfo.UserId,
                SessionId = sessionId,
                JoinedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining session {SessionId} for connection {ConnectionId}",
                sessionId, Context.ConnectionId);
        }
    }

    /// <summary>
    /// Leave a transcription session
    /// </summary>
    /// <param name="sessionId">The transcription session ID to leave</param>
    public async Task LeaveSession(string sessionId)
    {
        try
        {
            var connectionInfo = GetConnectionInfo();
            if (connectionInfo == null) return;

            // Remove from session group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session_{sessionId}");

            // Update connection info
            lock (_connectionsLock)
            {
                if (_activeConnections.TryGetValue(Context.ConnectionId, out var info))
                {
                    info.SessionId = string.Empty;
                }
            }

            _logger.LogInformation("User {UserId} left session {SessionId}", connectionInfo.UserId, sessionId);

            // Notify other users in the session
            await Clients.Group($"session_{sessionId}").SendAsync("UserLeftSession", new
            {
                UserId = connectionInfo.UserId,
                SessionId = sessionId,
                LeftAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving session {SessionId} for connection {ConnectionId}",
                sessionId, Context.ConnectionId);
        }
    }

    /// <summary>
    /// Send transcription update to session participants
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="transcriptionData">The transcription data</param>
    public async Task SendTranscriptionUpdate(string sessionId, object transcriptionData)
    {
        try
        {
            var connectionInfo = GetConnectionInfo();
            if (connectionInfo == null) return;

            // Only authenticated users can send transcription updates
            if (connectionInfo.UserRole == UserRole.Anonymous)
            {
                _logger.LogWarning("Anonymous user {UserId} attempted to send transcription update for session {SessionId}",
                    connectionInfo.UserId, sessionId);
                return;
            }

            // Verify user is in the session
            if (connectionInfo.SessionId != sessionId)
            {
                _logger.LogWarning("User {UserId} attempted to send update for session {SessionId} but is not in that session",
                    connectionInfo.UserId, sessionId);
                return;
            }

            _logger.LogDebug("Transcription update from user {UserId} for session {SessionId}",
                connectionInfo.UserId, sessionId);

            // Broadcast to all users in the session
            await Clients.Group($"session_{sessionId}").SendAsync("TranscriptionUpdate", new
            {
                SessionId = sessionId,
                UserId = connectionInfo.UserId,
                Data = transcriptionData,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending transcription update for session {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Send session status update
    /// </summary>
    /// <param name="sessionId">The session ID</param>
    /// <param name="status">The session status</param>
    /// <param name="message">Optional status message</param>
    public async Task SendSessionStatusUpdate(string sessionId, string status, string? message = null)
    {
        try
        {
            var connectionInfo = GetConnectionInfo();
            if (connectionInfo == null) return;

            // Only authenticated users can send session status updates
            if (connectionInfo.UserRole == UserRole.Anonymous)
            {
                _logger.LogWarning("Anonymous user {UserId} attempted to send session status update for session {SessionId}",
                    connectionInfo.UserId, sessionId);
                return;
            }

            _logger.LogInformation("Session status update: {Status} for session {SessionId} by user {UserId}",
                status, sessionId, connectionInfo.UserId);

            // Broadcast to all users in the session
            await Clients.Group($"session_{sessionId}").SendAsync("SessionStatusUpdate", new
            {
                SessionId = sessionId,
                Status = status,
                Message = message,
                UpdatedBy = connectionInfo.UserId,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending session status update for session {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Send notification to organization members
    /// </summary>
    /// <param name="notificationType">Type of notification</param>
    /// <param name="message">Notification message</param>
    /// <param name="data">Optional notification data</param>
    public async Task SendOrganizationNotification(string notificationType, string message, object? data = null)
    {
        try
        {
            var connectionInfo = GetConnectionInfo();
            if (connectionInfo == null) return;

            // Only authenticated admins can send organization notifications
            if (connectionInfo.UserRole != UserRole.OrganizationAdmin)
            {
                _logger.LogWarning("Non-admin user {UserId} attempted to send organization notification",
                    connectionInfo.UserId);
                return;
            }

            _logger.LogInformation("Organization notification sent by {UserId}: {Type} - {Message}",
                connectionInfo.UserId, notificationType, message);

            // Broadcast to all users in the organization
            await Clients.Group($"org_{connectionInfo.OrganizationId}").SendAsync("OrganizationNotification", new
            {
                Type = notificationType,
                Message = message,
                Data = data,
                SentBy = connectionInfo.UserId,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending organization notification");
        }
    }

    /// <summary>
    /// Get current connection information
    /// </summary>
    private ConnectionInfo? GetConnectionInfo()
    {
        lock (_connectionsLock)
        {
            return _activeConnections.TryGetValue(Context.ConnectionId, out var info) ? info : null;
        }
    }

    /// <summary>
    /// Get active connections for an organization (admin only)
    /// </summary>
    public async Task GetActiveConnections()
    {
        try
        {
            var connectionInfo = GetConnectionInfo();
            if (connectionInfo == null) return;

            // Only authenticated admins can view active connections
            if (connectionInfo.UserRole != UserRole.OrganizationAdmin)
            {
                _logger.LogWarning("Non-admin user {UserId} attempted to get active connections",
                    connectionInfo.UserId);
                return;
            }

            lock (_connectionsLock)
            {
                var orgConnections = _activeConnections.Values
                    .Where(c => c.OrganizationId == connectionInfo.OrganizationId && c.IsActive)
                    .Select(c => new
                    {
                        c.ConnectionId,
                        c.UserId,
                        c.SessionId,
                        c.ConnectedAt,
                        c.UserRole
                    })
                    .ToList();

                Clients.Caller.SendAsync("ActiveConnections", orgConnections);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active connections");
        }
    }

    /// <summary>
    /// Ping method for connection health checks
    /// </summary>
    public async Task Ping()
    {
        await Clients.Caller.SendAsync("Pong", DateTime.UtcNow);
    }
}
