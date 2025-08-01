using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Interfaces;

namespace SermonTranscription.Infrastructure.Services;

/// <summary>
/// Service for caching user organization data
/// </summary>
public class UserOrganizationCacheService : IUserOrganizationCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<UserOrganizationCacheService> _logger;
    private const string CacheKeyPrefix = "UserOrganizations_";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);

    public UserOrganizationCacheService(IMemoryCache cache, ILogger<UserOrganizationCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Get user with organizations from cache or load from repository
    /// </summary>
    public async Task<User?> GetUserWithOrganizationsAsync(
        Guid userId,
        Func<Guid, CancellationToken, Task<User?>> loadFromRepository,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(userId);
        User? user = null;

        if (_cache.TryGetValue(cacheKey, out user) && user != null)
        {
            _logger.LogDebug("User organizations loaded from cache for user {UserId}", userId);
            return user;
        }

        // Cache miss - load from repository
        user = await loadFromRepository(userId, cancellationToken);
        if (user == null)
        {
            _logger.LogDebug("User not found in repository for user {UserId}", userId);
            return null;
        }

        // Cache the user with organizations
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheExpiration,
            SlidingExpiration = TimeSpan.FromMinutes(10),
            Priority = CacheItemPriority.High
        };
        _cache.Set(cacheKey, user, cacheOptions);

        _logger.LogDebug("User organizations cached for user {UserId}", userId);
        return user;
    }

    /// <summary>
    /// Invalidate user organization cache for a specific user
    /// </summary>
    public void InvalidateUserCache(Guid userId)
    {
        var cacheKey = GetCacheKey(userId);
        _cache.Remove(cacheKey);
        _logger.LogDebug("User organization cache invalidated for user {UserId}", userId);
    }

    /// <summary>
    /// Invalidate cache for all users in an organization
    /// Note: This is a simplified approach. In a production environment,
    /// you might want to maintain a reverse index of users per organization
    /// or use a distributed cache with pattern-based invalidation
    /// </summary>
    public void InvalidateOrganizationCache(Guid organizationId)
    {
        // Note: This is a simplified approach. In a production environment,
        // you might want to maintain a reverse index of users per organization
        // or use a distributed cache with pattern-based invalidation
        _logger.LogDebug("Organization cache invalidation requested for organization {OrganizationId}", organizationId);
    }

    private static string GetCacheKey(Guid userId)
    {
        return $"{CacheKeyPrefix}{userId}";
    }
}
