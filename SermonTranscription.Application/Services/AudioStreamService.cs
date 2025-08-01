using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Interfaces;
using SermonTranscription.Application.Common;
using SermonTranscription.Domain.Interfaces;
using SermonTranscription.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace SermonTranscription.Application.Services;

/// <summary>
/// Service for handling audio streaming operations
/// </summary>
public class AudioStreamService : IAudioStreamService
{
    private readonly ITranscriptionSessionService _sessionService;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ILogger<AudioStreamService> _logger;
    private readonly IMemoryCache _cache;
    private const string CacheKeyPrefix = "AudioStream_";

    public AudioStreamService(
        ITranscriptionSessionService sessionService,
        IOrganizationRepository organizationRepository,
        ILogger<AudioStreamService> logger,
        IMemoryCache cache)
    {
        _sessionService = sessionService;
        _organizationRepository = organizationRepository;
        _logger = logger;
        _cache = cache;
    }

    private static string GetCacheKey(Guid sessionId) => $"{CacheKeyPrefix}{sessionId}";

    public async Task<ServiceResult<AudioStreamStatusResponse>> StartAudioStreamAsync(
        StartAudioStreamRequest request,
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate session exists and user has access
            var sessionResult = await _sessionService.GetSessionAsync(request.SessionId, organizationId, cancellationToken);
            if (!sessionResult.IsSuccess)
            {
                return ServiceResult<AudioStreamStatusResponse>.Failure("Session not found or access denied");
            }

            var session = sessionResult.Data!;

            // Check if session is active
            if (session.Status != SessionStatus.InProgress)
            {
                return ServiceResult<AudioStreamStatusResponse>.Failure("Session is not active and cannot receive audio");
            }

            // Check if stream is already active
            var cacheKey = GetCacheKey(request.SessionId);
            if (_cache.TryGetValue(cacheKey, out AudioStreamSession? existingStream))
            {
                return ServiceResult<AudioStreamStatusResponse>.Failure("Audio stream is already active for this session");
            }

            // Create new stream session
            var streamSession = new AudioStreamSession
            {
                SessionId = request.SessionId,
                OrganizationId = organizationId,
                UserId = userId,
                AudioFormat = request.AudioFormat,
                SampleRate = request.SampleRate,
                Channels = request.Channels,
                StartedAt = DateTime.UtcNow,
                IsActive = true,
                SessionStatus = session.Status,
                SessionCreatedAt = session.CreatedAt,
                SessionUpdatedAt = session.UpdatedAt
            };

            // Cache with expiration (4 hours max session duration)
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4),
                SlidingExpiration = TimeSpan.FromMinutes(30), // Extend if accessed
                Priority = CacheItemPriority.High
            };

            _cache.Set(cacheKey, streamSession, cacheOptions);

            _logger.LogInformation("Audio stream started for session {SessionId} by user {UserId} (session data cached for performance)",
                request.SessionId, userId);

            var status = new AudioStreamStatusResponse
            {
                SessionId = request.SessionId,
                IsActive = true,
                Status = session.Status.ToString(),
                CanReceiveAudio = true,
                LastActivityAt = DateTime.UtcNow,
                SupportsWebSocket = true,
                SupportsChunkedUpload = true,
                MaxChunkSizeBytes = 10 * 1024 * 1024, // 10MB
                SupportedAudioFormats = new[] { "wav", "mp3", "m4a", "flac" },
                WebSocketUrl = $"/api/v1/audio/{request.SessionId}/stream",
                UploadUrl = $"/api/v1/audio/{request.SessionId}/stream"
            };

            return ServiceResult<AudioStreamStatusResponse>.Success(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting audio stream for session {SessionId}", request.SessionId);
            return ServiceResult<AudioStreamStatusResponse>.Failure($"Failed to start audio stream: {ex.Message}");
        }
    }

    public async Task<ServiceResult<AudioChunkResponse>> ProcessAudioChunkAsync(
        Guid sessionId,
        byte[] audioData,
        int chunkIndex,
        bool isFinalChunk,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if stream is active and get cached session info
            var cacheKey = GetCacheKey(sessionId);
            AudioStreamSession? activeStreamSession = null;

            if (!_cache.TryGetValue(cacheKey, out activeStreamSession) || activeStreamSession == null || !activeStreamSession.IsActive)
            {
                // Cache miss or inactive stream - try to load from repository and recreate stream
                var sessionResult = await _sessionService.GetSessionAsync(sessionId, organizationId, cancellationToken);
                if (!sessionResult.IsSuccess)
                {
                    return ServiceResult<AudioChunkResponse>.Failure("Session not found or access denied");
                }

                var session = sessionResult.Data!;

                // Check if session is active
                if (session.Status != SessionStatus.InProgress)
                {
                    return ServiceResult<AudioChunkResponse>.Failure("Session is not active and cannot receive audio");
                }

                // Recreate the stream session in cache
                activeStreamSession = new AudioStreamSession
                {
                    SessionId = sessionId,
                    OrganizationId = organizationId,
                    UserId = Guid.Empty, // We don't have the original user ID, but this is acceptable for processing
                    AudioFormat = "unknown", // Will be updated with actual format
                    SampleRate = 16000, // Default sample rate
                    Channels = 1, // Default channels
                    StartedAt = DateTime.UtcNow,
                    IsActive = true,
                    SessionStatus = session.Status,
                    SessionCreatedAt = session.CreatedAt,
                    SessionUpdatedAt = session.UpdatedAt
                };

                // Cache the recreated session
                var recreateCacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4),
                    SlidingExpiration = TimeSpan.FromMinutes(30),
                    Priority = CacheItemPriority.High
                };
                _cache.Set(cacheKey, activeStreamSession, recreateCacheOptions);

                _logger.LogInformation("Audio stream session recreated from cache miss for session {SessionId}", sessionId);
            }

            // Validate organization access using session info
            if (activeStreamSession.OrganizationId != organizationId)
            {
                return ServiceResult<AudioChunkResponse>.Failure("Session not found or access denied");
            }

            // Check if session is active using session status
            if (activeStreamSession.SessionStatus != SessionStatus.InProgress)
            {
                return ServiceResult<AudioChunkResponse>.Failure("Session is not active and cannot receive audio");
            }

            // Validate audio data
            if (audioData.Length == 0)
            {
                return ServiceResult<AudioChunkResponse>.Failure("No audio data provided");
            }

            // Validate chunk size (max 10MB)
            const int maxChunkSize = 10 * 1024 * 1024; // 10MB
            if (audioData.Length > maxChunkSize)
            {
                return ServiceResult<AudioChunkResponse>.Failure($"Audio chunk too large. Maximum size is {maxChunkSize} bytes");
            }

            // Update stream session
            activeStreamSession.LastActivityAt = DateTime.UtcNow;
            activeStreamSession.TotalChunksReceived++;
            activeStreamSession.TotalBytesReceived += audioData.Length;
            activeStreamSession.LastChunkIndex = chunkIndex;

            // Update cache with new data (this extends the sliding expiration)
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4),
                SlidingExpiration = TimeSpan.FromMinutes(30),
                Priority = CacheItemPriority.High
            };
            _cache.Set(cacheKey, activeStreamSession, cacheOptions);

            _logger.LogDebug("Audio chunk {ChunkIndex} processed for session {SessionId} ({SizeBytes} bytes, Final: {IsFinalChunk})",
                chunkIndex, sessionId, audioData.Length, isFinalChunk);

            // TODO: Send to Gladia AI for transcription processing
            // This will be implemented in the next sub-task (4.4)

            var response = new AudioChunkResponse
            {
                ChunkIndex = chunkIndex,
                Success = true,
                SizeBytes = audioData.Length,
                ProcessedAt = DateTime.UtcNow
            };

            return ServiceResult<AudioChunkResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing audio chunk {ChunkIndex} for session {SessionId}", chunkIndex, sessionId);
            return ServiceResult<AudioChunkResponse>.Failure($"Failed to process audio chunk: {ex.Message}");
        }
    }

    /// <summary>
    /// Refreshes cached session data for a specific stream session
    /// This should be called periodically for long-running sessions or when session status might change
    /// </summary>
    public async Task<ServiceResult<bool>> RefreshSessionDataAsync(
        Guid sessionId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current session data from database
            var sessionResult = await _sessionService.GetSessionAsync(sessionId, organizationId, cancellationToken);
            if (!sessionResult.IsSuccess)
            {
                return ServiceResult<bool>.Failure("Session not found or access denied");
            }

            var session = sessionResult.Data!;
            var cacheKey = GetCacheKey(sessionId);

            // Update cached session data
            if (_cache.TryGetValue(cacheKey, out AudioStreamSession? streamSession) && streamSession != null)
            {
                streamSession.SessionStatus = session.Status;
                streamSession.SessionUpdatedAt = session.UpdatedAt;

                // If session is no longer active, stop the stream
                if (session.Status != SessionStatus.InProgress)
                {
                    streamSession.IsActive = false;
                    streamSession.StoppedAt = DateTime.UtcNow;
                    _cache.Remove(cacheKey);

                    _logger.LogInformation("Audio stream stopped for session {SessionId} due to session status change to {Status}",
                        sessionId, session.Status);
                }
                else
                {
                    // Update cache with refreshed data
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4),
                        SlidingExpiration = TimeSpan.FromMinutes(30),
                        Priority = CacheItemPriority.High
                    };
                    _cache.Set(cacheKey, streamSession, cacheOptions);
                }
            }

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing session data for session {SessionId}", sessionId);
            return ServiceResult<bool>.Failure($"Failed to refresh session data: {ex.Message}");
        }
    }

    public async Task<ServiceResult<AudioStreamStatusResponse>> GetAudioStreamStatusAsync(
        Guid sessionId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate session exists and user has access
            var sessionResult = await _sessionService.GetSessionAsync(sessionId, organizationId, cancellationToken);
            if (!sessionResult.IsSuccess)
            {
                return ServiceResult<AudioStreamStatusResponse>.Failure("Session not found or access denied");
            }

            var session = sessionResult.Data!;

            // Get stream session info from cache
            var cacheKey = GetCacheKey(sessionId);
            _cache.TryGetValue(cacheKey, out AudioStreamSession? streamSession);

            var status = new AudioStreamStatusResponse
            {
                SessionId = sessionId,
                IsActive = streamSession?.IsActive == true,
                Status = session.Status.ToString(),
                CanReceiveAudio = session.Status == SessionStatus.InProgress && streamSession?.IsActive == true,
                LastActivityAt = streamSession?.LastActivityAt ?? session.UpdatedAt ?? session.CreatedAt,
                SupportsWebSocket = true,
                SupportsChunkedUpload = true,
                MaxChunkSizeBytes = 10 * 1024 * 1024, // 10MB
                SupportedAudioFormats = new[] { "wav", "mp3", "m4a", "flac" },
                WebSocketUrl = $"/api/v1/audio/{sessionId}/stream",
                UploadUrl = $"/api/v1/audio/{sessionId}/stream"
            };

            return ServiceResult<AudioStreamStatusResponse>.Success(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audio stream status for session {SessionId}", sessionId);
            return ServiceResult<AudioStreamStatusResponse>.Failure($"Failed to get audio stream status: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> StopAudioStreamAsync(
        Guid sessionId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate session exists and user has access
            var sessionResult = await _sessionService.GetSessionAsync(sessionId, organizationId, cancellationToken);
            if (!sessionResult.IsSuccess)
            {
                return ServiceResult<bool>.Failure("Session not found or access denied");
            }

            // Stop stream session
            var cacheKey = GetCacheKey(sessionId);
            if (_cache.TryGetValue(cacheKey, out AudioStreamSession? streamSession) && streamSession != null)
            {
                streamSession.IsActive = false;
                streamSession.StoppedAt = DateTime.UtcNow;
                _cache.Remove(cacheKey);

                _logger.LogInformation("Audio stream stopped for session {SessionId} by user {UserId}",
                    sessionId, streamSession.UserId);
            }

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping audio stream for session {SessionId}", sessionId);
            return ServiceResult<bool>.Failure($"Failed to stop audio stream: {ex.Message}");
        }
    }

    public async Task<ServiceResult<AudioStreamConfiguration>> GetAudioStreamConfigurationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate organization exists
            var organization = await _organizationRepository.GetByIdAsync(organizationId, cancellationToken);
            if (organization == null)
            {
                return ServiceResult<AudioStreamConfiguration>.Failure("Organization not found");
            }

            var configuration = new AudioStreamConfiguration
            {
                MaxChunkSizeBytes = 10 * 1024 * 1024, // 10MB
                MaxSessionDuration = TimeSpan.FromHours(4),
                SupportedFormats = new[] { "wav", "mp3", "m4a", "flac" },
                SupportedSampleRates = new[] { 8000, 16000, 22050, 44100, 48000 },
                EnableRealTimeTranscription = true,
                EnableAudioBuffering = true,
                BufferSizeSeconds = 5
            };

            return ServiceResult<AudioStreamConfiguration>.Success(configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audio stream configuration for organization {OrganizationId}", organizationId);
            return ServiceResult<AudioStreamConfiguration>.Failure($"Failed to get audio stream configuration: {ex.Message}");
        }
    }
}

/// <summary>
/// Internal class for tracking active audio stream sessions
/// </summary>
internal class AudioStreamSession
{
    public Guid SessionId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public string AudioFormat { get; set; } = string.Empty;
    public int SampleRate { get; set; }
    public int Channels { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? StoppedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public bool IsActive { get; set; }
    public int TotalChunksReceived { get; set; }
    public long TotalBytesReceived { get; set; }
    public int LastChunkIndex { get; set; }

    // Cached session information to avoid repeated database queries
    public SessionStatus SessionStatus { get; set; }
    public DateTime SessionCreatedAt { get; set; }
    public DateTime? SessionUpdatedAt { get; set; }
}
