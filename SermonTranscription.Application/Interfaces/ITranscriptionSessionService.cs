using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Common;
using SermonTranscription.Domain.Entities;

namespace SermonTranscription.Application.Interfaces;

public interface ITranscriptionSessionService
{
    // CRUD operations
    Task<ServiceResult<TranscriptionSessionResponse>> CreateSessionAsync(
        CreateTranscriptionSessionRequest request,
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<TranscriptionSessionResponse>> GetSessionAsync(
        Guid sessionId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<TranscriptionSessionResponse>> UpdateSessionAsync(
        Guid sessionId,
        UpdateTranscriptionSessionRequest request,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> DeleteSessionAsync(
        Guid sessionId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    // Session management
    Task<ServiceResult<TranscriptionSessionResponse>> StartSessionAsync(
        Guid sessionId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<TranscriptionSessionResponse>> PauseSessionAsync(
        Guid sessionId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<TranscriptionSessionResponse>> ResumeSessionAsync(
        Guid sessionId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<TranscriptionSessionResponse>> CompleteSessionAsync(
        Guid sessionId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<TranscriptionSessionResponse>> CancelSessionAsync(
        Guid sessionId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    // Search and listing
    Task<ServiceResult<TranscriptionSessionListResponse>> SearchSessionsAsync(
        TranscriptionSessionSearchRequest request,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<IEnumerable<TranscriptionSessionResponse>>> GetRecentSessionsAsync(
        Guid organizationId,
        int count = 10,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<IEnumerable<TranscriptionSessionResponse>>> GetActiveSessionsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    // Analytics
    Task<ServiceResult<int>> GetActiveSessionCountAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<TimeSpan>> GetTotalSessionDurationAsync(
        Guid organizationId,
        DateTime? fromDate = null,
        CancellationToken cancellationToken = default);
}
