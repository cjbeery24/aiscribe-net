using SermonTranscription.Domain.Entities;

namespace SermonTranscription.Domain.Interfaces;

/// <summary>
/// Repository interface for User entity with user-specific operations
/// </summary>
public interface IUserRepository : IBaseRepository<User>
{
    // User-specific query methods
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithOrganizationsAsync(Guid userId, CancellationToken cancellationToken = default);

    // Authentication-related methods
    Task<User?> GetByEmailVerificationTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default);

    // User management methods
    Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetInactiveUsersAsync(CancellationToken cancellationToken = default);
    Task UpdateLastLoginAsync(Guid userId, DateTime lastLogin, CancellationToken cancellationToken = default);

    // Refresh token methods
    Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<IEnumerable<RefreshToken>> GetUserRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task RevokeRefreshTokenAsync(string token, CancellationToken cancellationToken = default);
    Task RevokeAllUserRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default);
    Task RevokeExpiredRefreshTokensAsync(CancellationToken cancellationToken = default);
}
