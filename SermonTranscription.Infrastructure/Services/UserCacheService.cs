using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Interfaces;

namespace SermonTranscription.Infrastructure.Services;

/// <summary>
/// Service for caching user data
/// </summary>
public class UserCacheService : IUserCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<UserCacheService> _logger;
    private const string CacheKeyPrefix = "User_";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);

    public UserCacheService(IMemoryCache cache, ILogger<UserCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Get user from cache or load from repository
    /// </summary>
    public async Task<User?> GetUserAsync(
        Guid userId,
        Func<Guid, CancellationToken, Task<User?>> loadFromRepository,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(userId);
        User? user = null;

        if (_cache.TryGetValue(cacheKey, out user) && user != null)
        {
            _logger.LogDebug("User loaded from cache for user {UserId}", userId);
            return user;
        }

        // Cache miss - load from repository
        user = await loadFromRepository(userId, cancellationToken);
        if (user == null)
        {
            _logger.LogDebug("User not found in repository for user {UserId}", userId);
            return null;
        }

        // Cache the user
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheExpiration,
            SlidingExpiration = TimeSpan.FromMinutes(10),
            Priority = CacheItemPriority.High
        };
        _cache.Set(cacheKey, user, cacheOptions);

        _logger.LogDebug("User cached for user {UserId}", userId);
        return user;
    }

    /// <summary>
    /// Invalidate user cache for a specific user
    /// </summary>
    public void InvalidateUserCache(Guid userId)
    {
        var cacheKey = GetCacheKey(userId);
        _cache.Remove(cacheKey);
        _logger.LogDebug("User cache invalidated for user {UserId}", userId);
    }

    private static string GetCacheKey(Guid userId)
    {
        return $"{CacheKeyPrefix}{userId}";
    }
}
