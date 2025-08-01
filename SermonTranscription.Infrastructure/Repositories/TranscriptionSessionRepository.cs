using Microsoft.EntityFrameworkCore;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Interfaces;
using SermonTranscription.Domain.Common;
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

    public async Task<PaginatedResult<TranscriptionSession>> SearchSessionsAsync(
        Guid organizationId,
        PaginationRequest paginationRequest,
        string? searchTerm = null,
        SessionStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool? isLive = null,
        string? language = null,
        Guid? createdByUserId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.TranscriptionSessions
            .Include(ts => ts.CreatedByUser)
            .Include(ts => ts.Transcriptions)
            .Where(ts => ts.OrganizationId == organizationId);

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchTermLower = searchTerm.ToLower();
            query = query.Where(ts =>
                ts.Title.ToLower().Contains(searchTermLower) ||
                (ts.Description != null && ts.Description.ToLower().Contains(searchTermLower)));
        }

        if (status.HasValue)
            query = query.Where(ts => ts.Status == status.Value);

        if (startDate.HasValue)
            query = query.Where(ts => ts.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(ts => ts.CreatedAt <= endDate.Value);

        if (isLive.HasValue)
            query = query.Where(ts => ts.IsLive == isLive.Value);

        if (!string.IsNullOrWhiteSpace(language))
            query = query.Where(ts => ts.Language == language);

        if (createdByUserId.HasValue)
            query = query.Where(ts => ts.CreatedByUserId == createdByUserId.Value);

        // Get total count before applying pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        var sortBy = paginationRequest.SortBy ?? "CreatedAt";
        query = sortBy.ToLower() switch
        {
            "title" => paginationRequest.SortDescending ? query.OrderByDescending(ts => ts.Title) : query.OrderBy(ts => ts.Title),
            "status" => paginationRequest.SortDescending ? query.OrderByDescending(ts => ts.Status) : query.OrderBy(ts => ts.Status),
            "startedat" => paginationRequest.SortDescending ? query.OrderByDescending(ts => ts.StartedAt) : query.OrderBy(ts => ts.StartedAt),
            "completedat" => paginationRequest.SortDescending ? query.OrderByDescending(ts => ts.CompletedAt) : query.OrderBy(ts => ts.CompletedAt),
            _ => paginationRequest.SortDescending ? query.OrderByDescending(ts => ts.CreatedAt) : query.OrderBy(ts => ts.CreatedAt)
        };

        // Apply pagination
        var sessions = await query
            .Skip((paginationRequest.PageNumber - 1) * paginationRequest.PageSize)
            .Take(paginationRequest.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<TranscriptionSession>
        {
            Items = sessions,
            TotalCount = totalCount,
            PageNumber = paginationRequest.PageNumber,
            PageSize = paginationRequest.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / paginationRequest.PageSize),
            HasNextPage = paginationRequest.PageNumber < (int)Math.Ceiling((double)totalCount / paginationRequest.PageSize),
            HasPreviousPage = paginationRequest.PageNumber > 1
        };
    }
}
