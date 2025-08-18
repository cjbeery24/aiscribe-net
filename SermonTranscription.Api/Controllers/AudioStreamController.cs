using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SermonTranscription.Api.Authorization;
using SermonTranscription.Api.Hubs;
using SermonTranscription.Api.Middleware;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Interfaces;
using System.IO;
using System.Text.Json;

namespace SermonTranscription.Api.Controllers;

/// <summary>
/// Controller for handling audio streaming endpoints for live transcription sessions
/// </summary>
[Route("api/v{version:apiVersion}/audio")]
[ApiVersion("1.0")]
public class AudioStreamController : BaseAuthenticatedApiController
{
    private readonly IAudioStreamService _audioStreamService;
    private readonly IHubContext<TranscriptionHub> _hubContext;
    private readonly ILogger<AudioStreamController> _audioLogger;

    public AudioStreamController(
        IAudioStreamService audioStreamService,
        IHubContext<TranscriptionHub> hubContext,
        ILogger<AudioStreamController> logger)
        : base(logger)
    {
        _audioStreamService = audioStreamService;
        _hubContext = hubContext;
        _audioLogger = logger;
    }

    /// <summary>
    /// Stream audio data to a live transcription session
    /// </summary>
    /// <param name="sessionId">The transcription session ID</param>
    /// <param name="chunkIndex">Sequential chunk index for ordering</param>
    /// <param name="isFinalChunk">Whether this is the final audio chunk</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Streaming response with transcription updates</returns>
    [HttpPost("{sessionId:guid}/stream")]
    [ProducesResponseType(typeof(ApiSuccessResponse<AudioChunkResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StreamAudio(
        Guid sessionId,
        [FromQuery] int chunkIndex = 0,
        [FromQuery] bool isFinalChunk = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var organizationId = HttpContext.GetOrganizationId()!.Value;

            // Read audio data from request body
            using var memoryStream = new MemoryStream();
            await Request.Body.CopyToAsync(memoryStream, cancellationToken);
            var audioData = memoryStream.ToArray();

            if (audioData.Length == 0)
            {
                return BadRequest(ApiErrorResponse.Create("No audio data provided"));
            }

            // Process audio chunk using service
            var result = await _audioStreamService.ProcessAudioChunkAsync(
                sessionId, audioData, chunkIndex, isFinalChunk, organizationId, cancellationToken);

            if (!result.IsSuccess)
            {
                return HandleServiceResult(result, () => Ok(result.Data));
            }

            // Send to SignalR hub for real-time processing
            await _hubContext.Clients.Group($"session_{sessionId:guid}").SendAsync("AudioChunkReceived", new
            {
                SessionId = sessionId,
                ChunkIndex = chunkIndex,
                SizeBytes = audioData.Length,
                IsFinalChunk = isFinalChunk,
                Timestamp = DateTime.UtcNow
            }, cancellationToken);

            _audioLogger.LogInformation("Audio chunk {ChunkIndex} received for session {SessionId} ({SizeBytes} bytes, Final: {IsFinalChunk})",
                chunkIndex, sessionId, audioData.Length, isFinalChunk);

            return SuccessResponse(result.Data, "Audio chunk processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing audio chunk for session {SessionId}", sessionId);
            return StatusCode(500, ApiErrorResponse.Create("Internal server error processing audio"));
        }
    }

    /// <summary>
    /// Start audio streaming for a session (WebSocket endpoint)
    /// </summary>
    /// <param name="sessionId">The transcription session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>WebSocket connection for real-time audio streaming</returns>
    [HttpGet("{sessionId:guid}/stream")]
    [ProducesResponseType(StatusCodes.Status101SwitchingProtocols)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartAudioStream(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var organizationId = HttpContext.GetOrganizationId()!.Value;
            var userId = HttpContext.GetUserId()!.Value;

            // Check if WebSocket is requested
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                return BadRequest(ApiErrorResponse.Create("WebSocket connection required"));
            }

            // Start audio stream using service
            var startRequest = new StartAudioStreamRequest
            {
                SessionId = sessionId,
                AudioFormat = "wav", // Default format, can be updated via WebSocket messages
                SampleRate = 16000,  // Default sample rate
                Channels = 1,        // Default mono
                UseWebSocket = true
            };

            var result = await _audioStreamService.StartAudioStreamAsync(
                startRequest, organizationId, userId, cancellationToken);

            if (!result.IsSuccess)
            {
                return HandleServiceResult(result, () => Ok(result.Data));
            }

            // Accept WebSocket connection
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            _logger.LogInformation("WebSocket audio stream started for session {SessionId}", sessionId);

            // Send connection confirmation
            var connectionMessage = JsonSerializer.Serialize(new
            {
                Type = "ConnectionEstablished",
                SessionId = sessionId,
                Timestamp = DateTime.UtcNow
            });

            var buffer = System.Text.Encoding.UTF8.GetBytes(connectionMessage);
            await webSocket.SendAsync(new ArraySegment<byte>(buffer), System.Net.WebSockets.WebSocketMessageType.Text, true, cancellationToken);

            // Handle WebSocket communication
            await HandleWebSocketStream(webSocket, sessionId, cancellationToken);

            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting audio stream for session {SessionId}", sessionId);
            return StatusCode(500, ApiErrorResponse.Create("Internal server error starting audio stream"));
        }
    }

    /// <summary>
    /// Handle WebSocket audio streaming
    /// </summary>
    private async Task HandleWebSocketStream(
        System.Net.WebSockets.WebSocket webSocket,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 1024]; // 1MB buffer
        var chunkIndex = 0;

        try
        {
            while (webSocket.State == System.Net.WebSockets.WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
                {
                    _logger.LogInformation("WebSocket connection closed for session {SessionId}", sessionId);
                    break;
                }

                if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Text)
                {
                    // Handle text messages (control messages)
                    var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await HandleTextMessage(message, sessionId, webSocket, cancellationToken);
                }
                else if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Binary)
                {
                    // Handle binary audio data
                    var audioData = new byte[result.Count];
                    Array.Copy(buffer, audioData, result.Count);

                    await ProcessAudioChunk(audioData, sessionId, chunkIndex++, false, cancellationToken);

                    // Send acknowledgment
                    var ackMessage = JsonSerializer.Serialize(new
                    {
                        Type = "AudioChunkAcknowledged",
                        ChunkIndex = chunkIndex - 1,
                        Timestamp = DateTime.UtcNow
                    });

                    var ackBuffer = System.Text.Encoding.UTF8.GetBytes(ackMessage);
                    await webSocket.SendAsync(new ArraySegment<byte>(ackBuffer), System.Net.WebSockets.WebSocketMessageType.Text, true, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in WebSocket stream for session {SessionId}", sessionId);
        }
        finally
        {
            if (webSocket.State == System.Net.WebSockets.WebSocketState.Open)
            {
                await webSocket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Stream ended", cancellationToken);
            }
        }
    }

    /// <summary>
    /// Handle text messages from WebSocket
    /// </summary>
    private async Task HandleTextMessage(
        string message,
        Guid sessionId,
        System.Net.WebSockets.WebSocket webSocket,
        CancellationToken cancellationToken)
    {
        try
        {
            var controlMessage = JsonSerializer.Deserialize<AudioControlMessage>(message);

            switch (controlMessage?.Type)
            {
                case "EndStream":
                    _logger.LogInformation("Audio stream ended for session {SessionId}", sessionId);
                    await webSocket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Stream ended", cancellationToken);
                    break;

                case "Ping":
                    var pongMessage = JsonSerializer.Serialize(new { Type = "Pong", Timestamp = DateTime.UtcNow });
                    var pongBuffer = System.Text.Encoding.UTF8.GetBytes(pongMessage);
                    await webSocket.SendAsync(new ArraySegment<byte>(pongBuffer), System.Net.WebSockets.WebSocketMessageType.Text, true, cancellationToken);
                    break;

                default:
                    _logger.LogWarning("Unknown control message type: {MessageType} for session {SessionId}", controlMessage?.Type, sessionId);
                    break;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON message received for session {SessionId}: {Message}", sessionId, message);
        }
    }

    /// <summary>
    /// Process audio chunk data
    /// </summary>
    private async Task ProcessAudioChunk(
        byte[] audioData,
        Guid sessionId,
        int chunkIndex,
        bool isFinalChunk,
        CancellationToken cancellationToken)
    {
        var organizationId = HttpContext.GetOrganizationId()!.Value;

        // Process audio chunk using service
        var result = await _audioStreamService.ProcessAudioChunkAsync(
            sessionId, audioData, chunkIndex, isFinalChunk, organizationId, cancellationToken);

        if (result.IsSuccess)
        {
            // Send to SignalR hub for real-time processing
            await _hubContext.Clients.Group($"session_{sessionId:guid}").SendAsync("AudioChunkReceived", new
            {
                SessionId = sessionId,
                ChunkIndex = chunkIndex,
                SizeBytes = audioData.Length,
                IsFinalChunk = isFinalChunk,
                Timestamp = DateTime.UtcNow
            }, cancellationToken);

            _logger.LogDebug("Audio chunk {ChunkIndex} processed for session {SessionId} ({SizeBytes} bytes, Final: {IsFinalChunk})",
                chunkIndex, sessionId, audioData.Length, isFinalChunk);
        }
        else
        {
            _logger.LogWarning("Failed to process audio chunk {ChunkIndex} for session {SessionId}: {Error}",
                chunkIndex, sessionId, result.Message);
        }
    }

    /// <summary>
    /// Start audio streaming for a session
    /// </summary>
    /// <param name="sessionId">The transcription session ID</param>
    /// <param name="request">Audio stream configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Audio stream status</returns>
    [HttpPost("{sessionId:guid}/start")]
    [ProducesResponseType(typeof(ApiSuccessResponse<AudioStreamStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartAudioStream(
        Guid sessionId,
        [FromBody] StartAudioStreamRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var organizationId = HttpContext.GetOrganizationId()!.Value;
            var userId = HttpContext.GetUserId()!.Value;

            // Ensure the session ID in the request matches the URL
            request.SessionId = sessionId;

            var result = await _audioStreamService.StartAudioStreamAsync(
                request, organizationId, userId, cancellationToken);

            return HandleServiceResult(result, () => SuccessResponse(result.Data, "Audio stream started successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting audio stream for session {SessionId}", sessionId);
            return StatusCode(500, ApiErrorResponse.Create("Internal server error starting audio stream"));
        }
    }

    /// <summary>
    /// Stop audio streaming for a session
    /// </summary>
    /// <param name="sessionId">The transcription session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("{sessionId:guid}/stop")]
    [ProducesResponseType(typeof(ApiSuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StopAudioStream(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var organizationId = HttpContext.GetOrganizationId()!.Value;

            var result = await _audioStreamService.StopAudioStreamAsync(
                sessionId, organizationId, cancellationToken);

            return HandleServiceResult(result, () => SuccessResponse("Audio stream stopped successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping audio stream for session {SessionId}", sessionId);
            return StatusCode(500, ApiErrorResponse.Create("Internal server error stopping audio stream"));
        }
    }

    /// <summary>
    /// Get audio stream status for a session
    /// </summary>
    /// <param name="sessionId">The transcription session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Audio stream status information</returns>
    [HttpGet("{sessionId:guid}/status")]
    [ProducesResponseType(typeof(ApiSuccessResponse<AudioStreamStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAudioStreamStatus(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var organizationId = HttpContext.GetOrganizationId()!.Value;

            var result = await _audioStreamService.GetAudioStreamStatusAsync(
                sessionId, organizationId, cancellationToken);

            return HandleServiceResult(result, () => SuccessResponse(result.Data, "Audio stream status retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audio stream status for session {SessionId}", sessionId);
            return StatusCode(500, ApiErrorResponse.Create("Internal server error getting stream status"));
        }
    }

    /// <summary>
    /// Get audio stream configuration for the organization
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Audio stream configuration</returns>
    [HttpGet("configuration")]
    [ProducesResponseType(typeof(ApiSuccessResponse<AudioStreamConfiguration>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAudioStreamConfiguration(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var organizationId = HttpContext.GetOrganizationId()!.Value;

            var result = await _audioStreamService.GetAudioStreamConfigurationAsync(
                organizationId, cancellationToken);

            return HandleServiceResult(result, () => SuccessResponse(result.Data, "Audio stream configuration retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audio stream configuration for organization");
            return StatusCode(500, ApiErrorResponse.Create("Internal server error getting configuration"));
        }
    }
}
