using SermonTranscription.Domain.Entities;

namespace SermonTranscription.Domain.Interfaces;

/// <summary>
/// Repository interface for TranscriptionSegment entity with segment-specific operations
/// </summary>
public interface ITranscriptionSegmentRepository : IBaseRepository<TranscriptionSegment>
{
    // Transcription-specific queries
    Task<IEnumerable<TranscriptionSegment>> GetByTranscriptionAsync(Guid transcriptionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TranscriptionSegment>> GetByTranscriptionOrderedAsync(Guid transcriptionId, CancellationToken cancellationToken = default);
    
    // Sequence-based operations
    Task<TranscriptionSegment?> GetBySequenceNumberAsync(Guid transcriptionId, int sequenceNumber, CancellationToken cancellationToken = default);
    Task<TranscriptionSegment?> GetNextSegmentAsync(Guid transcriptionId, int currentSequenceNumber, CancellationToken cancellationToken = default);
    Task<TranscriptionSegment?> GetPreviousSegmentAsync(Guid transcriptionId, int currentSequenceNumber, CancellationToken cancellationToken = default);
    Task<int> GetMaxSequenceNumberAsync(Guid transcriptionId, CancellationToken cancellationToken = default);
    
    // Time-based queries
    Task<IEnumerable<TranscriptionSegment>> GetByTimeRangeAsync(
        Guid transcriptionId, 
        TimeSpan startTime, 
        TimeSpan endTime, 
        CancellationToken cancellationToken = default);
    
    // Search within segments
    Task<IEnumerable<TranscriptionSegment>> SearchTextAsync(
        Guid transcriptionId, 
        string searchTerm, 
        CancellationToken cancellationToken = default);
    
    // Segment management
    Task<IEnumerable<TranscriptionSegment>> GetSegmentsAfterSequenceAsync(
        Guid transcriptionId, 
        int sequenceNumber, 
        CancellationToken cancellationToken = default);
    
    Task UpdateSequenceNumbersAsync(
        Guid transcriptionId, 
        int fromSequence, 
        int offset, 
        CancellationToken cancellationToken = default);
    
    // Analytics
    Task<int> GetSegmentCountAsync(Guid transcriptionId, CancellationToken cancellationToken = default);
    Task<TimeSpan> GetTotalDurationAsync(Guid transcriptionId, CancellationToken cancellationToken = default);
} 