using AutoMapper;
using Microsoft.Extensions.Logging;
using SermonTranscription.Application.Common;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Interfaces;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Exceptions;
using SermonTranscription.Domain.Interfaces;

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
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IUserOrganizationRepository userOrganizationRepository,
        IPasswordHasher passwordHasher,
        IPasswordValidator passwordValidator,
        IMapper mapper,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _userOrganizationRepository = userOrganizationRepository;
        _passwordHasher = passwordHasher;
        _passwordValidator = passwordValidator;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ServiceResult<UserProfileResponse>> GetUserProfileAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult<UserProfileResponse>.Failure("User not found");
            }

            var response = _mapper.Map<UserProfileResponse>(user);
            return ServiceResult<UserProfileResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile for user {UserId}", userId);
            return ServiceResult<UserProfileResponse>.Failure("An error occurred while retrieving the user profile");
        }
    }

    public async Task<ServiceResult<UserProfileResponse>> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequest request)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult<UserProfileResponse>.Failure("User not found");
            }

            // Update user properties (only the ones that exist in the User entity)
            if (!string.IsNullOrWhiteSpace(request.FirstName))
                user.FirstName = request.FirstName;
            if (!string.IsNullOrWhiteSpace(request.LastName))
                user.LastName = request.LastName;

            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            var response = _mapper.Map<UserProfileResponse>(user);
            return ServiceResult<UserProfileResponse>.Success(response, "User profile updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile for user {UserId}", userId);
            return ServiceResult<UserProfileResponse>.Failure("An error occurred while updating the user profile");
        }
    }

    public async Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult.Failure("User not found");
            }

            // Verify current password
            if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                return ServiceResult.Failure("Current password is incorrect");
            }

            // Validate new password
            try
            {
                _passwordValidator.Validate(request.NewPassword);
            }
            catch (PasswordValidationDomainException ex)
            {
                return ServiceResult.Failure(ex.Message);
            }

            // Hash new password
            user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Password changed successfully for user {UserId}", userId);
            return ServiceResult.Success("Password changed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            return ServiceResult.Failure("An error occurred while changing the password");
        }
    }

    public async Task<ServiceResult> DeactivateUserAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult.Failure("User not found");
            }

            user.Deactivate();
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("User {UserId} deactivated", userId);
            return ServiceResult.Success("User deactivated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user {UserId}", userId);
            return ServiceResult.Failure("An error occurred while deactivating the user");
        }
    }

    public async Task<ServiceResult> ActivateUserAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult.Failure("User not found");
            }

            user.Activate();
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("User {UserId} activated", userId);
            return ServiceResult.Success("User activated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user {UserId}", userId);
            return ServiceResult.Failure("An error occurred while activating the user");
        }
    }

    public async Task<ServiceResult<OrganizationUserListResponse>> GetOrganizationUsersAsync(Guid organizationId, OrganizationUserSearchRequest request)
    {
        try
        {
            var users = await _userOrganizationRepository.GetOrganizationUsersAsync(organizationId);
            var totalCount = await _userOrganizationRepository.GetActiveUserCountAsync(organizationId);

            // Apply filtering and pagination in memory for now
            var filteredUsers = users.Where(u =>
                (string.IsNullOrEmpty(request.SearchTerm) ||
                 u.User.FirstName.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                 u.User.LastName.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                 u.User.Email.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrEmpty(request.Role) || u.Role.ToString() == request.Role) &&
                (!request.IsActive.HasValue || u.IsActive == request.IsActive.Value) &&
                (!request.IsEmailVerified.HasValue || u.User.IsEmailVerified == request.IsEmailVerified.Value)
            ).ToList();

            var totalPages = (int)Math.Ceiling((double)filteredUsers.Count / request.PageSize);
            var hasNextPage = request.PageNumber < totalPages;
            var hasPreviousPage = request.PageNumber > 1;

            var pagedUsers = filteredUsers
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var response = new OrganizationUserListResponse
            {
                Users = pagedUsers.Select(u => _mapper.Map<OrganizationUserResponse>(u)).ToList(),
                TotalCount = filteredUsers.Count,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = hasNextPage,
                HasPreviousPage = hasPreviousPage
            };

            return ServiceResult<OrganizationUserListResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organization users for organization {OrganizationId}", organizationId);
            return ServiceResult<OrganizationUserListResponse>.Failure("An error occurred while retrieving organization users");
        }
    }

    public async Task<ServiceResult<OrganizationUserResponse>> GetOrganizationUserAsync(Guid organizationId, Guid userId)
    {
        try
        {
            var userOrg = await _userOrganizationRepository.GetUserOrganizationAsync(organizationId, userId);
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

    public async Task<ServiceResult<OrganizationUserResponse>> UpdateOrganizationUserRoleAsync(Guid organizationId, Guid userId, UpdateOrganizationUserRoleRequest request)
    {
        try
        {
            var userOrg = await _userOrganizationRepository.GetUserOrganizationAsync(organizationId, userId);
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
            await _userOrganizationRepository.UpdateAsync(userOrg);

            var response = _mapper.Map<OrganizationUserResponse>(userOrg);
            return ServiceResult<OrganizationUserResponse>.Success(response, "User role updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating organization user role {UserId} for organization {OrganizationId}", userId, organizationId);
            return ServiceResult<OrganizationUserResponse>.Failure("An error occurred while updating the user role");
        }
    }

    public async Task<ServiceResult> RemoveUserFromOrganizationAsync(Guid organizationId, Guid userId)
    {
        try
        {
            var userOrg = await _userOrganizationRepository.GetUserOrganizationAsync(organizationId, userId);
            if (userOrg == null)
            {
                return ServiceResult.Failure("User not found in organization");
            }

            await _userOrganizationRepository.DeleteAsync(userOrg);

            _logger.LogInformation("User {UserId} removed from organization {OrganizationId}", userId, organizationId);
            return ServiceResult.Success("User removed from organization successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user {UserId} from organization {OrganizationId}", userId, organizationId);
            return ServiceResult.Failure("An error occurred while removing the user from the organization");
        }
    }

    public async Task<ServiceResult> DeactivateOrganizationUserAsync(Guid organizationId, Guid userId)
    {
        try
        {
            var userOrg = await _userOrganizationRepository.GetUserOrganizationAsync(organizationId, userId);
            if (userOrg == null)
            {
                return ServiceResult.Failure("User not found in organization");
            }

            userOrg.Deactivate();
            await _userOrganizationRepository.UpdateAsync(userOrg);

            _logger.LogInformation("Organization user {UserId} deactivated in organization {OrganizationId}", userId, organizationId);
            return ServiceResult.Success("Organization user deactivated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating organization user {UserId} in organization {OrganizationId}", userId, organizationId);
            return ServiceResult.Failure("An error occurred while deactivating the organization user");
        }
    }

    public async Task<ServiceResult> ActivateOrganizationUserAsync(Guid organizationId, Guid userId)
    {
        try
        {
            var userOrg = await _userOrganizationRepository.GetUserOrganizationAsync(organizationId, userId);
            if (userOrg == null)
            {
                return ServiceResult.Failure("User not found in organization");
            }

            userOrg.Activate();
            await _userOrganizationRepository.UpdateAsync(userOrg);

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
