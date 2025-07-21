using System.ComponentModel.DataAnnotations;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Exceptions;

namespace SermonTranscription.Domain.Entities;

public class User
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsEmailVerified { get; set; }
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiry { get; set; }

    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<UserOrganization> UserOrganizations { get; set; } = new List<UserOrganization>();
    public ICollection<TranscriptionSession> TranscriptionSessions { get; set; } = new List<TranscriptionSession>();
    public ICollection<Transcription> CreatedTranscriptions { get; set; } = new List<Transcription>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    // Computed properties
    public string FullName => $"{FirstName} {LastName}";

    // Domain methods
    public void MarkEmailAsVerified()
    {
        IsEmailVerified = true;
        EmailVerificationToken = null;
        EmailVerificationTokenExpiry = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanResetPassword()
    {
        return !string.IsNullOrEmpty(PasswordResetToken) &&
               PasswordResetTokenExpiry.HasValue &&
               PasswordResetTokenExpiry.Value > DateTime.UtcNow;
    }

    public void ClearPasswordResetToken()
    {
        PasswordResetToken = null;
        PasswordResetTokenExpiry = null;
        UpdatedAt = DateTime.UtcNow;
    }

    // Organization-related helper methods

    /// <summary>
    /// Get user's membership in a specific organization
    /// </summary>
    public UserOrganization? GetOrganizationMembership(Guid organizationId)
    {
        return UserOrganizations.FirstOrDefault(uo => uo.OrganizationId == organizationId && uo.IsActive);
    }

    /// <summary>
    /// Get all organizations where the user is active
    /// </summary>
    public IEnumerable<Organization> GetActiveOrganizations()
    {
        return UserOrganizations
            .Where(uo => uo.IsActive)
            .Select(uo => uo.Organization);
    }

    /// <summary>
    /// Check if user is an admin in a specific organization
    /// </summary>
    public bool IsAdmin(Guid organizationId)
    {
        var membership = GetOrganizationMembership(organizationId);
        return membership?.IsAdmin() ?? false;
    }

    /// <summary>
    /// Check if user can manage users in a specific organization
    /// </summary>
    public bool CanManageUsers(Guid organizationId)
    {
        var membership = GetOrganizationMembership(organizationId);
        return membership?.CanManageUsers() ?? false;
    }

    /// <summary>
    /// Check if user can manage transcriptions in a specific organization
    /// </summary>
    public bool CanManageTranscriptions(Guid organizationId)
    {
        var membership = GetOrganizationMembership(organizationId);
        return membership?.CanManageTranscriptions() ?? false;
    }

    /// <summary>
    /// Check if user can view transcriptions in a specific organization
    /// </summary>
    public bool CanViewTranscriptions(Guid organizationId)
    {
        var membership = GetOrganizationMembership(organizationId);
        return IsActive && (membership?.CanViewTranscriptions() ?? false);
    }

    /// <summary>
    /// Check if user can export transcriptions from a specific organization
    /// </summary>
    public bool CanExportTranscriptions(Guid organizationId)
    {
        var membership = GetOrganizationMembership(organizationId);
        return membership?.CanExportTranscriptions() ?? false;
    }

    /// <summary>
    /// Update user's role in a specific organization
    /// </summary>
    public void UpdateRoleInOrganization(Guid organizationId, UserRole newRole)
    {
        var membership = GetOrganizationMembership(organizationId);
        if (membership != null)
        {
            membership.UpdateRole(newRole);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Join an organization with a specific role
    /// </summary>
    public void JoinOrganization(Guid organizationId, UserRole role = UserRole.OrganizationUser, Guid? invitedByUserId = null)
    {
        // Check if already a member
        var existingMembership = UserOrganizations.FirstOrDefault(uo => uo.OrganizationId == organizationId);
        if (existingMembership != null)
        {
            // Reactivate if was previously deactivated
            existingMembership.Activate();
            existingMembership.UpdateRole(role);
        }
        else
        {
            // Create new membership
            var membership = new UserOrganization
            {
                UserId = Id,
                OrganizationId = organizationId,
                Role = role,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                InvitedByUserId = invitedByUserId
            };
            UserOrganizations.Add(membership);
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Leave an organization (deactivate membership)
    /// </summary>
    public void LeaveOrganization(Guid organizationId)
    {
        var membership = GetOrganizationMembership(organizationId);
        if (membership != null)
        {
            membership.Deactivate();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsEmailVerificationExpired()
    {
        return EmailVerificationTokenExpiry.HasValue &&
               EmailVerificationTokenExpiry.Value < DateTime.UtcNow;
    }

    public void GenerateEmailVerificationToken()
    {
        EmailVerificationToken = Guid.NewGuid().ToString();
        EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        UpdatedAt = DateTime.UtcNow;
    }

    public void GeneratePasswordResetToken()
    {
        PasswordResetToken = Guid.NewGuid().ToString();
        PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        UpdatedAt = DateTime.UtcNow;
    }

    // Validation methods
    public void ValidateCanManageUsers(Guid organizationId)
    {
        if (!CanManageUsers(organizationId))
        {
            throw new UserPermissionException($"User {Email} does not have permission to manage users in organization {organizationId}.");
        }
    }

    public void ValidateCanManageTranscriptions(Guid organizationId)
    {
        if (!CanManageTranscriptions(organizationId))
        {
            throw new UserPermissionException($"User {Email} does not have permission to manage transcriptions in organization {organizationId}.");
        }
    }

    public void ValidateCanViewTranscriptions(Guid organizationId)
    {
        if (!CanViewTranscriptions(organizationId))
        {
            throw new UserPermissionException($"User {Email} does not have permission to view transcriptions in organization {organizationId}.");
        }
    }

    public void ValidateCanExportTranscriptions(Guid organizationId)
    {
        if (!CanExportTranscriptions(organizationId))
        {
            throw new UserPermissionException($"User {Email} does not have permission to export transcriptions in organization {organizationId}.");
        }
    }

    public void ValidateIsMemberOfOrganization(Guid organizationId)
    {
        var membership = GetOrganizationMembership(organizationId);
        if (membership == null)
        {
            throw new UserPermissionException($"User {Email} is not a member of organization {organizationId}.");
        }
    }

    public void ValidateIsActiveInOrganization(Guid organizationId)
    {
        var membership = GetOrganizationMembership(organizationId);
        if (membership == null || !membership.IsActive)
        {
            throw new UserPermissionException($"User {Email} is not active in organization {organizationId}.");
        }
    }

    public void ValidateEmailVerification()
    {
        if (!IsEmailVerified)
        {
            throw new UserEmailVerificationException($"User {Email} email is not verified.");
        }
    }

    public void ValidateIsActive()
    {
        if (!IsActive)
        {
            throw new UserAuthenticationException($"User {Email} account is not active.");
        }
    }

    public void ValidatePasswordResetToken()
    {
        if (!CanResetPassword())
        {
            throw new UserPasswordResetException($"Password reset token for user {Email} is invalid or expired.");
        }
    }
}
