using Microsoft.EntityFrameworkCore;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Interfaces;
using SermonTranscription.Infrastructure.Data;

namespace SermonTranscription.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Transcription entity
/// </summary>
public class TranscriptionRepository : BaseMultiTenantRepository<Transcription>, ITranscriptionRepository
{
    public TranscriptionRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Transcription>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.Transcriptions
            .Include(t => t.CreatedByUser)
            .Include(t => t.Session)
            .Where(t => t.OrganizationId == organizationId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transcription>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Transcriptions
            .Include(t => t.CreatedByUser)
            .Include(t => t.Session)
            .Where(t => t.CreatedByUserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Transcription?> GetWithSegmentsAsync(Guid transcriptionId, CancellationToken cancellationToken = default)
    {
        return await _context.Transcriptions
            .Include(t => t.CreatedByUser)
            .Include(t => t.Session)
            .Include(t => t.Segments.OrderBy(s => s.SequenceNumber))
            .FirstOrDefaultAsync(t => t.Id == transcriptionId, cancellationToken);
    }

    public async Task<IEnumerable<Transcription>> SearchByContentAsync(Guid organizationId, string searchTerm, CancellationToken cancellationToken = default)
    {
        return await _context.Transcriptions
            .Include(t => t.CreatedByUser)
            .Include(t => t.Session)
            .Where(t => t.OrganizationId == organizationId && t.Content.Contains(searchTerm))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transcription>> SearchByTitleAsync(Guid organizationId, string searchTerm, CancellationToken cancellationToken = default)
    {
        return await _context.Transcriptions
            .Include(t => t.CreatedByUser)
            .Include(t => t.Session)
            .Where(t => t.OrganizationId == organizationId && t.Title.Contains(searchTerm))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transcription>> GetByDateRangeAsync(Guid organizationId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Transcriptions
            .Include(t => t.CreatedByUser)
            .Include(t => t.Session)
            .Where(t => t.OrganizationId == organizationId && t.CreatedAt >= startDate && t.CreatedAt <= endDate)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transcription>> GetBySpeakerAsync(Guid organizationId, string speaker, CancellationToken cancellationToken = default)
    {
        return await _context.Transcriptions
            .Include(t => t.CreatedByUser)
            .Include(t => t.Session)
            .Where(t => t.OrganizationId == organizationId && t.Speaker == speaker)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transcription>> GetByTagsAsync(Guid organizationId, IEnumerable<string> tags, CancellationToken cancellationToken = default)
    {
        var tagArray = tags.ToArray();
        return await _context.Transcriptions
            .Include(t => t.CreatedByUser)
            .Include(t => t.Session)
            .Where(t => t.OrganizationId == organizationId && t.Tags != null && t.Tags.Any(tag => tagArray.Contains(tag)))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transcription>> GetBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.Transcriptions
            .Include(t => t.CreatedByUser)
            .Include(t => t.Session)
            .Where(t => t.SessionId == sessionId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transcription>> GetRecentTranscriptionsAsync(Guid organizationId, int count = 10, CancellationToken cancellationToken = default)
    {
        return await _context.Transcriptions
            .Include(t => t.CreatedByUser)
            .Include(t => t.Session)
            .Where(t => t.OrganizationId == organizationId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTranscriptionCountAsync(Guid organizationId, DateTime? fromDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Transcriptions.Where(t => t.OrganizationId == organizationId);

        if (fromDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= fromDate.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<TimeSpan> GetTotalDurationAsync(Guid organizationId, DateTime? fromDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Transcriptions.Where(t => t.OrganizationId == organizationId && t.DurationSeconds.HasValue);

        if (fromDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= fromDate.Value);
        }

        var totalSeconds = await query.SumAsync(t => t.DurationSeconds!.Value, cancellationToken);
        return TimeSpan.FromSeconds(totalSeconds);
    }

    public async Task<IEnumerable<Transcription>> GetExportableTranscriptionsAsync(Guid organizationId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Transcriptions
            .Include(t => t.CreatedByUser)
            .Include(t => t.Session)
            .Where(t => t.OrganizationId == organizationId);

        if (fromDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= toDate.Value);
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
