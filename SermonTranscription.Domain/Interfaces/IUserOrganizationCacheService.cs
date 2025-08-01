using SermonTranscription.Domain.Entities;

namespace SermonTranscription.Domain.Interfaces;

/// <summary>
/// Service for caching user organization data
/// </summary>
public interface IUserOrganizationCacheService
{
    /// <summary>
    /// Get user with organizations from cache or load from repository
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="loadFromRepository">Function to load user data if not in cache</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user with organizations, or null if not found</returns>
    Task<User?> GetUserWithOrganizationsAsync(
        Guid userId,
        Func<Guid, CancellationToken, Task<User?>> loadFromRepository,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidate user organization cache for a specific user
    /// </summary>
    /// <param name="userId">The user ID whose cache should be invalidated</param>
    void InvalidateUserCache(Guid userId);

    /// <summary>
    /// Invalidate cache for all users in an organization
    /// Note: This is a simplified approach. In a production environment,
    /// you might want to maintain a reverse index of users per organization
    /// or use a distributed cache with pattern-based invalidation
    /// </summary>
    /// <param name="organizationId">The organization ID whose users' cache should be invalidated</param>
    void InvalidateOrganizationCache(Guid organizationId);
}
