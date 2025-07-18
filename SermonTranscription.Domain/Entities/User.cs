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
    
    // Role and permissions
    public UserRole Role { get; set; } = UserRole.OrganizationUser;
    
    // Navigation properties
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    
    public ICollection<TranscriptionSession> TranscriptionSessions { get; set; } = new List<TranscriptionSession>();
    public ICollection<Transcription> CreatedTranscriptions { get; set; } = new List<Transcription>();
    
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
    
    // Role-based permission methods
    public bool IsAdmin()
    {
        return Role == UserRole.OrganizationAdmin;
    }
    
    public bool CanManageUsers()
    {
        return Role == UserRole.OrganizationAdmin;
    }
    
    public bool CanManageTranscriptions()
    {
        return Role == UserRole.OrganizationAdmin || Role == UserRole.OrganizationUser;
    }
    
    public bool CanViewTranscriptions()
    {
        return IsActive && (Role == UserRole.OrganizationAdmin || 
                           Role == UserRole.OrganizationUser || 
                           Role == UserRole.ReadOnlyUser);
    }
    
    public bool CanExportTranscriptions()
    {
        return Role == UserRole.OrganizationAdmin || Role == UserRole.OrganizationUser;
    }
    
    public void UpdateRole(UserRole newRole)
    {
        Role = newRole;
        UpdatedAt = DateTime.UtcNow;
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
    public void ValidateCanManageUsers()
    {
        if (!CanManageUsers())
        {
            throw new UserPermissionException($"User {Email} does not have permission to manage users.");
        }
    }
    
    public void ValidateCanManageTranscriptions()
    {
        if (!CanManageTranscriptions())
        {
            throw new UserPermissionException($"User {Email} does not have permission to manage transcriptions.");
        }
    }
    
    public void ValidateCanViewTranscriptions()
    {
        if (!CanViewTranscriptions())
        {
            throw new UserPermissionException($"User {Email} does not have permission to view transcriptions.");
        }
    }
    
    public void ValidateCanExportTranscriptions()
    {
        if (!CanExportTranscriptions())
        {
            throw new UserPermissionException($"User {Email} does not have permission to export transcriptions.");
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