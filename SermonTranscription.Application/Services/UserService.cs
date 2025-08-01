using AutoMapper;
using Microsoft.Extensions.Logging;
using SermonTranscription.Application.Common;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Interfaces;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Exceptions;
using SermonTranscription.Domain.Interfaces;
using SermonTranscription.Domain.Common;

namespace SermonTranscription.Application.Services;

/// <summary>
/// Service for user profile management operations
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserOrganizationRepository _userOrganizationRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordValidator _passwordValidator;
    private readonly IMapper _mapper;
    private readonly IUserOrganizationCacheService _userOrganizationCacheService;
    private readonly IUserCacheService _userCacheService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IUserOrganizationRepository userOrganizationRepository,
        IPasswordHasher passwordHasher,
        IPasswordValidator passwordValidator,
        IMapper mapper,
        IUserOrganizationCacheService userOrganizationCacheService,
        IUserCacheService userCacheService,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _userOrganizationRepository = userOrganizationRepository;
        _passwordHasher = passwordHasher;
        _passwordValidator = passwordValidator;
        _mapper = mapper;
        _userOrganizationCacheService = userOrganizationCacheService;
        _userCacheService = userCacheService;
        _logger = logger;
    }

    public async Task<ServiceResult<UserProfileResponse>> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return ServiceResult<UserProfileResponse>.Failure("User not found", ErrorCode.NotFound);
            }

            var response = _mapper.Map<UserProfileResponse>(user);
            return ServiceResult<UserProfileResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile for user {UserId}", userId);
            return ServiceResult<UserProfileResponse>.Failure("An error occurred while retrieving the user profile", ErrorCode.InternalError);
        }
    }

    public async Task<ServiceResult<UserProfileResponse>> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return ServiceResult<UserProfileResponse>.Failure("User not found", ErrorCode.NotFound);
            }

            // Update user properties (only the ones that exist in the User entity)
            if (!string.IsNullOrWhiteSpace(request.FirstName))
                user.FirstName = request.FirstName;
            if (!string.IsNullOrWhiteSpace(request.LastName))
                user.LastName = request.LastName;

            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);

            // Invalidate user cache
            _userCacheService.InvalidateUserCache(userId);

            var response = _mapper.Map<UserProfileResponse>(user);
            return ServiceResult<UserProfileResponse>.Success(response, "User profile updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile for user {UserId}", userId);
            return ServiceResult<UserProfileResponse>.Failure("An error occurred while updating the user profile", ErrorCode.InternalError);
        }
    }

    public async Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return ServiceResult.Failure("User not found", ErrorCode.NotFound);
            }

            // Verify current password
            if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                return ServiceResult.Failure("Current password is incorrect", ErrorCode.Unauthorized);
            }

            // Validate new password
            try
            {
                _passwordValidator.Validate(request.NewPassword);
            }
            catch (PasswordValidationDomainException ex)
            {
                return ServiceResult.Failure(ex.Message, ErrorCode.ValidationError, "newPassword");
            }

            // Hash new password
            user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);

            // Invalidate user cache
            _userCacheService.InvalidateUserCache(userId);

            _logger.LogInformation("Password changed successfully for user {UserId}", userId);
            return ServiceResult.Success("Password changed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            return ServiceResult.Failure("An error occurred while changing the password", ErrorCode.InternalError);
        }
    }

    public async Task<ServiceResult> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return ServiceResult.Failure("User not found");
            }

            user.Deactivate();
            await _userRepository.UpdateAsync(user, cancellationToken);

            // Invalidate user cache
            _userCacheService.InvalidateUserCache(userId);

            _logger.LogInformation("User {UserId} deactivated", userId);
            return ServiceResult.Success("User deactivated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user {UserId}", userId);
            return ServiceResult.Failure("An error occurred while deactivating the user");
        }
    }

    public async Task<ServiceResult> ActivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return ServiceResult.Failure("User not found");
            }

            user.Activate();
            await _userRepository.UpdateAsync(user, cancellationToken);

            // Invalidate user cache
            _userCacheService.InvalidateUserCache(userId);

            _logger.LogInformation("User {UserId} activated", userId);
            return ServiceResult.Success("User activated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user {UserId}", userId);
            return ServiceResult.Failure("An error occurred while activating the user");
        }
    }

    public async Task<ServiceResult<OrganizationUserListResponse>> GetOrganizationUsersAsync(Guid organizationId, OrganizationUserSearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create pagination request
            var paginationRequest = new PaginationRequest
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                SortDescending = request.SortDescending
            };

            // Get paginated results from repository
            var paginatedResult = await _userOrganizationRepository.GetPaginatedOrganizationUsersAsync(
                organizationId,
                paginationRequest,
                request.SearchTerm,
                request.Role,
                request.IsActive,
                request.IsEmailVerified,
                cancellationToken);

            // Map to DTOs
            var users = paginatedResult.Items.Select(u => _mapper.Map<OrganizationUserResponse>(u)).ToList();

            var response = new OrganizationUserListResponse
            {
                Users = users,
                TotalCount = paginatedResult.TotalCount,
                PageNumber = paginatedResult.PageNumber,
                PageSize = paginatedResult.PageSize,
                TotalPages = paginatedResult.TotalPages,
                HasNextPage = paginatedResult.HasNextPage,
                HasPreviousPage = paginatedResult.HasPreviousPage
            };

            return ServiceResult<OrganizationUserListResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organization users for organization {OrganizationId}", organizationId);
            return ServiceResult<OrganizationUserListResponse>.Failure("An error occurred while retrieving organization users");
        }
    }

    public async Task<ServiceResult<OrganizationUserResponse>> GetOrganizationUserAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userOrg = await _userOrganizationRepository.GetUserOrganizationAsync(userId, organizationId, cancellationToken);
            if (userOrg == null)
            {
                return ServiceResult<OrganizationUserResponse>.Failure("User not found in organization");
            }

            var response = _mapper.Map<OrganizationUserResponse>(userOrg);
            return ServiceResult<OrganizationUserResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organization user {UserId} for organization {OrganizationId}", userId, organizationId);
            return ServiceResult<OrganizationUserResponse>.Failure("An error occurred while retrieving the organization user");
        }
    }

    public async Task<ServiceResult<OrganizationUserResponse>> UpdateOrganizationUserRoleAsync(Guid organizationId, Guid userId, UpdateOrganizationUserRoleRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userOrg = await _userOrganizationRepository.GetUserOrganizationAsync(userId, organizationId, cancellationToken);
            if (userOrg == null)
            {
                return ServiceResult<OrganizationUserResponse>.Failure("User not found in organization");
            }

            // Validate role
            if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            {
                return ServiceResult<OrganizationUserResponse>.Failure("Invalid role specified");
            }

            userOrg.UpdateRole(role);
            await _userOrganizationRepository.UpdateAsync(userOrg, cancellationToken);

            // Invalidate user cache since user's role changed
            _userOrganizationCacheService.InvalidateUserCache(userId);

            var response = _mapper.Map<OrganizationUserResponse>(userOrg);
            return ServiceResult<OrganizationUserResponse>.Success(response, "User role updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating organization user role {UserId} for organization {OrganizationId}", userId, organizationId);
            return ServiceResult<OrganizationUserResponse>.Failure("An error occurred while updating the user role");
        }
    }

    public async Task<ServiceResult> RemoveUserFromOrganizationAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userOrg = await _userOrganizationRepository.GetUserOrganizationAsync(userId, organizationId, cancellationToken);
            if (userOrg == null)
            {
                return ServiceResult.Failure("User not found in organization");
            }

            await _userOrganizationRepository.DeleteAsync(userOrg, cancellationToken);

            // Invalidate user cache since user was removed from organization
            _userOrganizationCacheService.InvalidateUserCache(userId);

            _logger.LogInformation("User {UserId} removed from organization {OrganizationId}", userId, organizationId);
            return ServiceResult.Success("User removed from organization successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user {UserId} from organization {OrganizationId}", userId, organizationId);
            return ServiceResult.Failure("An error occurred while removing the user from the organization");
        }
    }

    public async Task<ServiceResult> DeactivateOrganizationUserAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userOrg = await _userOrganizationRepository.GetUserOrganizationAsync(userId, organizationId, cancellationToken);
            if (userOrg == null)
            {
                return ServiceResult.Failure("User not found in organization");
            }

            userOrg.Deactivate();
            await _userOrganizationRepository.UpdateAsync(userOrg, cancellationToken);

            // Invalidate user cache since user's organization membership status changed
            _userOrganizationCacheService.InvalidateUserCache(userId);

            _logger.LogInformation("Organization user {UserId} deactivated in organization {OrganizationId}", userId, organizationId);
            return ServiceResult.Success("Organization user deactivated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating organization user {UserId} in organization {OrganizationId}", userId, organizationId);
            return ServiceResult.Failure("An error occurred while deactivating the organization user");
        }
    }

    public async Task<ServiceResult> ActivateOrganizationUserAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userOrg = await _userOrganizationRepository.GetUserOrganizationAsync(userId, organizationId, cancellationToken);
            if (userOrg == null)
            {
                return ServiceResult.Failure("User not found in organization");
            }

            userOrg.Activate();
            await _userOrganizationRepository.UpdateAsync(userOrg, cancellationToken);

            _logger.LogInformation("Organization user {UserId} activated in organization {OrganizationId}", userId, organizationId);
            return ServiceResult.Success("Organization user activated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating organization user {UserId} in organization {OrganizationId}", userId, organizationId);
            return ServiceResult.Failure("An error occurred while activating the organization user");
        }
    }
}
