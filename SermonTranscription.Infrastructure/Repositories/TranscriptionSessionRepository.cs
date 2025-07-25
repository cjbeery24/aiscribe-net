using Microsoft.EntityFrameworkCore;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Interfaces;
using SermonTranscription.Infrastructure.Data;

namespace SermonTranscription.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for TranscriptionSession entity
/// </summary>
public class TranscriptionSessionRepository : BaseMultiTenantRepository<TranscriptionSession>, ITranscriptionSessionRepository
{
    public TranscriptionSessionRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<TranscriptionSession>> GetByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.TranscriptionSessions
            .Include(ts => ts.CreatedByUser)
            .Include(ts => ts.Transcriptions)
            .Where(ts => ts.OrganizationId == organizationId)
            .OrderByDescending(ts => ts.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TranscriptionSession>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.TranscriptionSessions
            .Include(ts => ts.CreatedByUser)
            .Include(ts => ts.Transcriptions)
            .Where(ts => ts.CreatedByUserId == userId)
            .OrderByDescending(ts => ts.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TranscriptionSession>> GetByStatusAsync(SessionStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.TranscriptionSessions
            .Include(ts => ts.CreatedByUser)
            .Include(ts => ts.Transcriptions)
            .Where(ts => ts.Status == status)
            .OrderByDescending(ts => ts.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TranscriptionSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TranscriptionSessions
            .Include(ts => ts.CreatedByUser)
            .Include(ts => ts.Transcriptions)
            .Where(ts => ts.IsActive)
            .OrderByDescending(ts => ts.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<TranscriptionSession?> GetWithTranscriptionsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.TranscriptionSessions
            .Include(ts => ts.CreatedByUser)
            .Include(ts => ts.Transcriptions.OrderByDescending(t => t.CreatedAt))
            .FirstOrDefaultAsync(ts => ts.Id == sessionId, cancellationToken);
    }

    public async Task<IEnumerable<TranscriptionSession>> GetRecentSessionsAsync(Guid organizationId, int count = 10, CancellationToken cancellationToken = default)
    {
        return await _context.TranscriptionSessions
            .Include(ts => ts.CreatedByUser)
            .Include(ts => ts.Transcriptions)
            .Where(ts => ts.OrganizationId == organizationId)
            .OrderByDescending(ts => ts.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TranscriptionSession>> GetSessionsByDateRangeAsync(Guid organizationId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.TranscriptionSessions
            .Include(ts => ts.CreatedByUser)
            .Include(ts => ts.Transcriptions)
            .Where(ts => ts.OrganizationId == organizationId && ts.CreatedAt >= startDate && ts.CreatedAt <= endDate)
            .OrderByDescending(ts => ts.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetActiveSessionCountAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.TranscriptionSessions
            .CountAsync(ts => ts.OrganizationId == organizationId && ts.IsActive, cancellationToken);
    }

    public async Task<TimeSpan> GetTotalSessionDurationAsync(Guid organizationId, DateTime? fromDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.TranscriptionSessions
            .Where(ts => ts.OrganizationId == organizationId && ts.Duration.HasValue);

        if (fromDate.HasValue)
        {
            query = query.Where(ts => ts.CreatedAt >= fromDate.Value);
        }

        var totalTicks = await query.SumAsync(ts => ts.Duration!.Value.Ticks, cancellationToken);
        return TimeSpan.FromTicks(totalTicks);
    }
}
