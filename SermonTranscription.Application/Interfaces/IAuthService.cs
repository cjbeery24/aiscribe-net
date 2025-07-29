using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Common;

namespace SermonTranscription.Application.Interfaces;

/// <summary>
/// Service interface for authentication and authorization operations
/// </summary>
public interface IAuthService
{
    Task<ServiceResult<LoginResponse>> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<ServiceResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<RefreshResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<ServiceResult<LogoutResponse>> RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<ServiceResult<LogoutResponse>> RevokeAllUserRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ServiceResult<ForgotPasswordResponse>> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);
    Task<ServiceResult<ResetPasswordResponse>> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default);
    Task<ServiceResult<List<OrganizationSummaryDto>>> GetUserOrganizationsAsync(Guid userId, CancellationToken cancellationToken = default);
}
