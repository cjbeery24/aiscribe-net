using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Common;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Exceptions;
using SermonTranscription.Domain.Interfaces;

namespace SermonTranscription.Application.Services;

/// <summary>
/// Service for handling user invitations to organizations
/// </summary>
public class InvitationService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserOrganizationRepository _userOrganizationRepository;
    private readonly IEmailService _emailService;
    private readonly IJwtService _jwtService;
    private readonly ILogger<InvitationService> _logger;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordValidator _passwordValidator;

    public InvitationService(
        IUserRepository userRepository,
        IUserOrganizationRepository userOrganizationRepository,
        IEmailService emailService,
        IJwtService jwtService,
        ILogger<InvitationService> logger,
        IPasswordHasher passwordHasher,
        IPasswordValidator passwordValidator)
    {
        _userRepository = userRepository;
        _userOrganizationRepository = userOrganizationRepository;
        _emailService = emailService;
        _jwtService = jwtService;
        _logger = logger;
        _passwordHasher = passwordHasher;
        _passwordValidator = passwordValidator;
    }

    /// <summary>
    /// Invite a user to join an organization
    /// </summary>
    public async Task<ServiceResult<InviteUserResponse>> InviteUserAsync(
        InviteUserRequest request,
        Guid organizationId,
        Guid invitedByUserId)
    {
        try
        {
            // Validate the request
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return ServiceResult<InviteUserResponse>.Failure("Email address is required", "VALIDATION_ERROR", "email");
            }

            if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            {
                return ServiceResult<InviteUserResponse>.Failure("First name and last name are required", "VALIDATION_ERROR", "firstName");
            }

            // Check if user already exists
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);

            // Check if user is already a member of this organization
            if (existingUser != null)
            {
                var existingMembership = await _userOrganizationRepository.GetUserOrganizationAsync(
                    existingUser.Id, organizationId);

                if (existingMembership != null)
                {
                    return ServiceResult<InviteUserResponse>.Failure("User is already a member of this organization", "CONFLICT");
                }
            }

            // Get the inviting user's information
            var invitingUser = await _userRepository.GetByIdAsync(invitedByUserId);
            if (invitingUser == null)
            {
                return ServiceResult<InviteUserResponse>.Failure("Inviting user not found", "NOT_FOUND");
            }

            // Generate invitation token
            var invitationToken = GenerateInvitationToken();

            // Create or update user
            User user;
            if (existingUser == null)
            {
                // Create new user (without password - they'll set it when accepting invitation)
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PasswordHash = string.Empty, // Will be set when accepting invitation
                    IsEmailVerified = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _userRepository.AddAsync(user);
            }
            else
            {
                user = existingUser;
            }

            // Create user-organization relationship
            var userOrganization = new UserOrganization
            {
                UserId = user.Id,
                OrganizationId = organizationId,
                Role = ParseUserRole(request.Role),
                IsActive = false, // Will be activated when invitation is accepted
                InvitedByUserId = invitedByUserId,
                InvitationToken = invitationToken,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _userOrganizationRepository.AddAsync(userOrganization);

            // TODO: Send invitation email
            // await _emailService.SendInvitationEmailAsync(user.Email, invitationToken, organizationId);

            _logger.LogInformation("User {UserId} invited to organization {OrganizationId} by {InvitedByUserId}",
                user.Id, organizationId, invitedByUserId);

            var response = new InviteUserResponse
            {
                Success = true,
                Message = "Invitation sent successfully",
                Email = request.Email
            };

            return ServiceResult<InviteUserResponse>.Success(response, "Invitation sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inviting user {Email} to organization {OrganizationId}", request.Email, organizationId);
            return ServiceResult<InviteUserResponse>.Failure("An error occurred while sending the invitation", "INTERNAL_ERROR");
        }
    }

    /// <summary>
    /// Accept an invitation to join an organization
    /// </summary>
    public async Task<ServiceResult<AcceptInvitationResponse>> AcceptInvitationAsync(AcceptInvitationRequest request)
    {
        try
        {
            // Validate the request
            if (string.IsNullOrWhiteSpace(request.InvitationToken))
            {
                return ServiceResult<AcceptInvitationResponse>.Failure("Invitation token is required", "VALIDATION_ERROR", "invitationToken");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return ServiceResult<AcceptInvitationResponse>.Failure("Password is required", "VALIDATION_ERROR", "password");
            }

            // Validate password
            try
            {
                _passwordValidator.Validate(request.Password);
            }
            catch (PasswordValidationDomainException ex)
            {
                return ServiceResult<AcceptInvitationResponse>.Failure(ex.Message, "VALIDATION_ERROR", "password");
            }

            // Find the invitation
            var userOrganization = await _userOrganizationRepository.GetByInvitationTokenAsync(request.InvitationToken);
            if (userOrganization == null)
            {
                return ServiceResult<AcceptInvitationResponse>.Failure("Invalid invitation token", "NOT_FOUND");
            }

            // Check if invitation is expired (7 days)
            if (userOrganization.CreatedAt < DateTime.UtcNow.AddDays(-7))
            {
                return ServiceResult<AcceptInvitationResponse>.Failure("Invitation has expired", "UNAUTHORIZED");
            }

            // Check if invitation is already accepted
            if (userOrganization.InvitationAcceptedAt.HasValue)
            {
                return ServiceResult<AcceptInvitationResponse>.Failure("Invitation has already been accepted", "CONFLICT");
            }

            // Get the user
            var user = userOrganization.User ?? await _userRepository.GetByIdAsync(userOrganization.UserId);
            if (user == null)
            {
                return ServiceResult<AcceptInvitationResponse>.Failure("User not found", "NOT_FOUND");
            }

            // Hash the password
            var passwordHash = _passwordHasher.HashPassword(request.Password);

            // Update user
            user.PasswordHash = passwordHash;
            user.MarkEmailAsVerified();
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            // Activate the membership
            userOrganization.AcceptInvitation();

            await _userOrganizationRepository.UpdateAsync(userOrganization);

            _logger.LogInformation("User {UserId} accepted invitation to organization {OrganizationId}",
                user.Id, userOrganization.OrganizationId);

            var acceptResponse = new AcceptInvitationResponse
            {
                Success = true,
                Message = "Invitation accepted successfully",
                OrganizationName = userOrganization.Organization.Name,
                Role = userOrganization.Role.ToString()
            };

            return ServiceResult<AcceptInvitationResponse>.Success(acceptResponse, "Invitation accepted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting invitation with token {Token}", request.InvitationToken);
            return ServiceResult<AcceptInvitationResponse>.Failure("An error occurred while accepting the invitation", "INTERNAL_ERROR");
        }
    }

    private static string GenerateInvitationToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private static UserRole ParseUserRole(string role)
    {
        return role?.ToLowerInvariant() switch
        {
            "admin" => UserRole.OrganizationAdmin,
            "user" => UserRole.OrganizationUser,
            _ => UserRole.OrganizationUser
        };
    }
}
