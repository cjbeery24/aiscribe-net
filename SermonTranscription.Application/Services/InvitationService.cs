using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using SermonTranscription.Application.DTOs;
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
    public async Task<InviteUserResponse> InviteUserAsync(
        InviteUserRequest request,
        Guid organizationId,
        Guid invitedByUserId)
    {
        try
        {
            // Validate the request
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                throw new UserDomainException("Email address is required.");
            }

            if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            {
                throw new UserDomainException("First name and last name are required.");
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
                    return new InviteUserResponse
                    {
                        Success = false,
                        Message = "User is already a member of this organization.",
                        Email = request.Email
                    };
                }
            }

            // Get the inviting user's information
            var invitingUser = await _userRepository.GetByIdAsync(invitedByUserId);
            if (invitingUser == null)
            {
                throw new UserDomainException("Inviting user not found.");
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
                    IsEmailVerified = false, // Will be verified when they accept invitation
                    CreatedAt = DateTime.UtcNow
                };

                await _userRepository.AddAsync(user);
            }
            else
            {
                user = existingUser;
            }

            // Create user organization relationship
            var userOrganization = new UserOrganization
            {
                UserId = user.Id,
                OrganizationId = organizationId,
                Role = ParseUserRole(request.Role),
                CreatedAt = DateTime.UtcNow,
                IsActive = false, // Will be activated when invitation is accepted
                InvitationToken = invitationToken,
                InvitedByUserId = invitedByUserId
            };

            await _userOrganizationRepository.AddAsync(userOrganization);

            // Send invitation email
            var emailSent = await _emailService.SendInvitationEmailAsync(
                user.Email,
                $"{user.FirstName} {user.LastName}",
                "Organization Name", // TODO: Get from organization repository
                $"{invitingUser.FirstName} {invitingUser.LastName}",
                invitationToken,
                request.Message);

            if (!emailSent)
            {
                _logger.LogWarning("Failed to send invitation email to {Email}", user.Email);
                // Continue anyway - the invitation is still created
            }

            return new InviteUserResponse
            {
                Success = true,
                Message = "Invitation sent successfully.",
                Email = user.Email,
                InvitationToken = invitationToken // For testing purposes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invite user {Email} to organization {OrganizationId}",
                request.Email, organizationId);
            throw;
        }
    }

    /// <summary>
    /// Accept an invitation to join an organization
    /// </summary>
    public async Task<AcceptInvitationResponse> AcceptInvitationAsync(AcceptInvitationRequest request)
    {
        try
        {
            // Validate the request
            if (string.IsNullOrWhiteSpace(request.InvitationToken))
            {
                throw new UserDomainException("Invitation token is required.");
            }

            // Validate password
            _passwordValidator.Validate(request.Password);

            // Find the invitation
            var userOrganization = await _userOrganizationRepository.GetByInvitationTokenAsync(request.InvitationToken);
            if (userOrganization == null)
            {
                return new AcceptInvitationResponse
                {
                    Success = false,
                    Message = "Invalid or expired invitation token."
                };
            }

            // Check if invitation has already been accepted
            if (userOrganization.InvitationAcceptedAt.HasValue)
            {
                return new AcceptInvitationResponse
                {
                    Success = false,
                    Message = "This invitation has already been accepted."
                };
            }

            // Check if invitation has expired (7 days)
            if (userOrganization.CreatedAt < DateTime.UtcNow.AddDays(-7))
            {
                return new AcceptInvitationResponse
                {
                    Success = false,
                    Message = "This invitation has expired."
                };
            }

            // Get the user
            var user = await _userRepository.GetByIdAsync(userOrganization.UserId);
            if (user == null)
            {
                throw new UserDomainException("User not found.");
            }

            // Set password and verify email
            user.PasswordHash = _passwordHasher.HashPassword(request.Password);
            user.MarkEmailAsVerified();
            await _userRepository.UpdateAsync(user);

            // Accept the invitation
            userOrganization.AcceptInvitation();
            await _userOrganizationRepository.UpdateAsync(userOrganization);

            // Send welcome email
            var emailSent = await _emailService.SendWelcomeEmailAsync(
                user.Email,
                $"{user.FirstName} {user.LastName}",
                "Organization Name"); // TODO: Get from organization repository

            if (!emailSent)
            {
                _logger.LogWarning("Failed to send welcome email to {Email}", user.Email);
            }

            // Generate JWT tokens
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken(user);

            return new AcceptInvitationResponse
            {
                Success = true,
                Message = "Invitation accepted successfully. Welcome!",
                OrganizationName = "Organization Name", // TODO: Get from organization repository
                Role = userOrganization.Role.ToString(),
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to accept invitation with token {Token}", request.InvitationToken);
            throw;
        }
    }

    /// <summary>
    /// Generate a secure invitation token
    /// </summary>
    private static string GenerateInvitationToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    /// <summary>
    /// Parse user role from string
    /// </summary>
    private static UserRole ParseUserRole(string role)
    {
        return role.ToLowerInvariant() switch
        {
            "admin" or "organizationadmin" => UserRole.OrganizationAdmin,
            "user" or "organizationuser" => UserRole.OrganizationUser,
            "readonly" or "readonlyuser" => UserRole.ReadOnlyUser,
            _ => UserRole.OrganizationUser
        };
    }
}
