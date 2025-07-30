using Microsoft.EntityFrameworkCore;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Interfaces;
using SermonTranscription.Domain.Common;
using SermonTranscription.Infrastructure.Data;

namespace SermonTranscription.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for UserOrganization entity operations
/// </summary>
public class UserOrganizationRepository : BaseRepository<UserOrganization>, IUserOrganizationRepository
{
    public UserOrganizationRepository(AppDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Get user's membership in a specific organization
    /// </summary>
    public async Task<UserOrganization?> GetUserOrganizationAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.UserOrganizations
            .Include(uo => uo.User)
            .Include(uo => uo.Organization)
            .FirstOrDefaultAsync(uo => uo.UserId == userId && uo.OrganizationId == organizationId, cancellationToken);
    }

    /// <summary>
    /// Get all organizations where user is active
    /// </summary>
    public async Task<IEnumerable<UserOrganization>> GetUserOrganizationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserOrganizations
            .Include(uo => uo.Organization)
            .Where(uo => uo.UserId == userId && uo.IsActive)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get paginated organizations where user is active
    /// </summary>
    public async Task<PaginatedResult<UserOrganization>> GetPaginatedUserOrganizationsAsync(Guid userId, PaginationRequest request, bool? isActive = null, string? role = null, CancellationToken cancellationToken = default)
    {
        // Build the base query
        var query = _context.UserOrganizations
            .Include(uo => uo.Organization)
            .Where(uo => uo.UserId == userId);

        // Apply filters
        if (isActive.HasValue)
        {
            query = query.Where(uo => uo.IsActive == isActive.Value);
        }

        if (!string.IsNullOrEmpty(role))
        {
            if (Enum.TryParse<UserRole>(role, true, out var roleEnum))
            {
                query = query.Where(uo => uo.Role == roleEnum);
            }
        }

        // Get total count before applying pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = request.SortBy?.ToLowerInvariant() switch
        {
            "name" => request.SortDescending
                ? query.OrderByDescending(uo => uo.Organization.Name)
                : query.OrderBy(uo => uo.Organization.Name),
            "createdat" => request.SortDescending
                ? query.OrderByDescending(uo => uo.CreatedAt)
                : query.OrderBy(uo => uo.CreatedAt),
            "updatedat" => request.SortDescending
                ? query.OrderByDescending(uo => uo.UpdatedAt)
                : query.OrderBy(uo => uo.UpdatedAt),
            "isactive" => request.SortDescending
                ? query.OrderByDescending(uo => uo.IsActive)
                : query.OrderBy(uo => uo.IsActive),
            "role" => request.SortDescending
                ? query.OrderByDescending(uo => uo.Role)
                : query.OrderBy(uo => uo.Role),
            _ => request.SortDescending
                ? query.OrderByDescending(uo => uo.Organization.Name)
                : query.OrderBy(uo => uo.Organization.Name)
        };

        // Apply pagination
        var skip = (request.PageNumber - 1) * request.PageSize;
        var items = await query
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Calculate pagination metadata
        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        return new PaginatedResult<UserOrganization>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = totalPages,
            HasNextPage = request.PageNumber < totalPages,
            HasPreviousPage = request.PageNumber > 1
        };
    }

    /// <summary>
    /// Get all active users in an organization
    /// </summary>
    public async Task<IEnumerable<UserOrganization>> GetOrganizationUsersAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.UserOrganizations
            .Include(uo => uo.User)
            .Where(uo => uo.OrganizationId == organizationId && uo.IsActive)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get users in an organization with specific role
    /// </summary>
    public async Task<IEnumerable<UserOrganization>> GetOrganizationUsersByRoleAsync(Guid organizationId, UserRole role, CancellationToken cancellationToken = default)
    {
        return await _context.UserOrganizations
            .Include(uo => uo.User)
            .Where(uo => uo.OrganizationId == organizationId && uo.Role == role && uo.IsActive)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get admin users in an organization
    /// </summary>
    public async Task<IEnumerable<UserOrganization>> GetOrganizationAdminsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await GetOrganizationUsersByRoleAsync(organizationId, UserRole.OrganizationAdmin, cancellationToken);
    }

    /// <summary>
    /// Check if user is member of organization
    /// </summary>
    public async Task<bool> IsUserMemberOfOrganizationAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.UserOrganizations
            .AnyAsync(uo => uo.UserId == userId && uo.OrganizationId == organizationId && uo.IsActive, cancellationToken);
    }

    /// <summary>
    /// Get pending invitations for a user
    /// </summary>
    public async Task<IEnumerable<UserOrganization>> GetPendingInvitationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserOrganizations
            .Include(uo => uo.Organization)
            .Where(uo => uo.UserId == userId && !uo.InvitationAcceptedAt.HasValue)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get pending invitations for an organization
    /// </summary>
    public async Task<IEnumerable<UserOrganization>> GetOrganizationPendingInvitationsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.UserOrganizations
            .Include(uo => uo.User)
            .Where(uo => uo.OrganizationId == organizationId && !uo.InvitationAcceptedAt.HasValue)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get user-organization relationship by invitation token
    /// </summary>
    public async Task<UserOrganization?> GetByInvitationTokenAsync(string invitationToken, CancellationToken cancellationToken = default)
    {
        return await _context.UserOrganizations
            .Include(uo => uo.User)
            .Include(uo => uo.Organization)
            .FirstOrDefaultAsync(uo => uo.InvitationToken == invitationToken, cancellationToken);
    }

    /// <summary>
    /// Get count of active users in organization
    /// </summary>
    public async Task<int> GetActiveUserCountAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.UserOrganizations
            .CountAsync(uo => uo.OrganizationId == organizationId && uo.IsActive, cancellationToken);
    }

    /// <summary>
    /// Get count of organizations for user
    /// </summary>
    public async Task<int> GetUserOrganizationCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserOrganizations
            .CountAsync(uo => uo.UserId == userId && uo.IsActive, cancellationToken);
    }
}
