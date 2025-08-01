using SermonTranscription.Domain.Entities;

namespace SermonTranscription.Domain.Interfaces;

/// <summary>
/// Service for caching user data
/// </summary>
public interface IUserCacheService
{
    /// <summary>
    /// Get user from cache or load from repository
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="loadFromRepository">Function to load user data if not in cache</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user, or null if not found</returns>
    Task<User?> GetUserAsync(
        Guid userId,
        Func<Guid, CancellationToken, Task<User?>> loadFromRepository,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidate user cache for a specific user
    /// </summary>
    /// <param name="userId">The user ID whose cache should be invalidated</param>
    void InvalidateUserCache(Guid userId);
}
