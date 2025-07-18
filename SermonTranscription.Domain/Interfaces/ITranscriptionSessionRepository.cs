using SermonTranscription.Domain.Entities;

namespace SermonTranscription.Domain.Interfaces;

/// <summary>
/// Repository interface for TranscriptionSession entity with session-specific operations
/// </summary>
public interface ITranscriptionSessionRepository : IBaseRepository<TranscriptionSession>
{
    // Session-specific query methods
    Task<IEnumerable<TranscriptionSession>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TranscriptionSession>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TranscriptionSession>> GetByStatusAsync(SessionStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<TranscriptionSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default);
    
    // Session management methods
    Task<TranscriptionSession?> GetWithTranscriptionsAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TranscriptionSession>> GetRecentSessionsAsync(Guid organizationId, int count = 10, CancellationToken cancellationToken = default);
    
    // Date-based queries
    Task<IEnumerable<TranscriptionSession>> GetSessionsByDateRangeAsync(
        Guid organizationId, 
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken = default);
    
    // Session analytics
    Task<int> GetActiveSessionCountAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<TimeSpan> GetTotalSessionDurationAsync(Guid organizationId, DateTime? fromDate = null, CancellationToken cancellationToken = default);
} 