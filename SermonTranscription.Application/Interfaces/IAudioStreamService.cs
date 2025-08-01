using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Common;

namespace SermonTranscription.Application.Interfaces;

/// <summary>
/// Service for handling audio streaming operations
/// </summary>
public interface IAudioStreamService
{
    /// <summary>
    /// Start audio streaming for a session
    /// </summary>
    /// <param name="request">Audio stream configuration</param>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Audio stream status</returns>
    Task<ServiceResult<AudioStreamStatusResponse>> StartAudioStreamAsync(
        StartAudioStreamRequest request,
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Process an audio chunk for a session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="audioData">Audio data bytes</param>
    /// <param name="chunkIndex">Chunk index</param>
    /// <param name="isFinalChunk">Whether this is the final chunk</param>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Processing result</returns>
    Task<ServiceResult<AudioChunkResponse>> ProcessAudioChunkAsync(
        Guid sessionId,
        byte[] audioData,
        int chunkIndex,
        bool isFinalChunk,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audio stream status for a session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Audio stream status</returns>
    Task<ServiceResult<AudioStreamStatusResponse>> GetAudioStreamStatusAsync(
        Guid sessionId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop audio streaming for a session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    Task<ServiceResult<bool>> StopAudioStreamAsync(
        Guid sessionId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audio stream configuration
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Audio stream configuration</returns>
    Task<ServiceResult<AudioStreamConfiguration>> GetAudioStreamConfigurationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes cached session data for a specific stream session
    /// This should be called periodically for long-running sessions or when session status might change
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    Task<ServiceResult<bool>> RefreshSessionDataAsync(
        Guid sessionId,
        Guid organizationId,
        CancellationToken cancellationToken = default);
}
