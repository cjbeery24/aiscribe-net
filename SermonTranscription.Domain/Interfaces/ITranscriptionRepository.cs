using SermonTranscription.Domain.Entities;

namespace SermonTranscription.Domain.Interfaces;

/// <summary>
/// Repository interface for Transcription entity with transcription-specific operations
/// </summary>
public interface ITranscriptionRepository : IBaseRepository<Transcription>
{
    // Organization-scoped queries
    Task<IEnumerable<Transcription>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transcription>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Transcription?> GetWithSegmentsAsync(Guid transcriptionId, CancellationToken cancellationToken = default);
    
    // Search functionality
    Task<IEnumerable<Transcription>> SearchByContentAsync(
        Guid organizationId, 
        string searchTerm, 
        CancellationToken cancellationToken = default);
    
    Task<IEnumerable<Transcription>> SearchByTitleAsync(
        Guid organizationId, 
        string searchTerm, 
        CancellationToken cancellationToken = default);
    
    // Filtering capabilities
    Task<IEnumerable<Transcription>> GetByDateRangeAsync(
        Guid organizationId, 
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken = default);
    
    Task<IEnumerable<Transcription>> GetBySpeakerAsync(
        Guid organizationId, 
        string speaker, 
        CancellationToken cancellationToken = default);
    
    Task<IEnumerable<Transcription>> GetByTagsAsync(
        Guid organizationId, 
        IEnumerable<string> tags, 
        CancellationToken cancellationToken = default);
    
    // Session-related queries
    Task<IEnumerable<Transcription>> GetBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transcription>> GetRecentTranscriptionsAsync(
        Guid organizationId, 
        int count = 10, 
        CancellationToken cancellationToken = default);
    
    // Analytics and statistics
    Task<int> GetTranscriptionCountAsync(Guid organizationId, DateTime? fromDate = null, CancellationToken cancellationToken = default);
    Task<TimeSpan> GetTotalDurationAsync(Guid organizationId, DateTime? fromDate = null, CancellationToken cancellationToken = default);
    
    // Export functionality
    Task<IEnumerable<Transcription>> GetExportableTranscriptionsAsync(
        Guid organizationId, 
        DateTime? fromDate = null, 
        DateTime? toDate = null, 
        CancellationToken cancellationToken = default);
} 