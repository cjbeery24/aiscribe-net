using System.ComponentModel.DataAnnotations;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Exceptions;

namespace SermonTranscription.Domain.Entities;

/// <summary>
/// Join entity representing a user's membership in an organization with specific role
/// </summary>
public class UserOrganization
{
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    
    /// <summary>
    /// The user's role within this specific organization
    /// </summary>
    public UserRole Role { get; set; } = UserRole.OrganizationUser;
    
    /// <summary>
    /// When the user joined this organization
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When the user's role or membership was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Whether the user is active in this organization
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Optional invitation token used when user was invited
    /// </summary>
    public string? InvitationToken { get; set; }
    
    /// <summary>
    /// When the user accepted the invitation (if applicable)
    /// </summary>
    public DateTime? InvitationAcceptedAt { get; set; }
    
    /// <summary>
    /// Who invited this user to the organization
    /// </summary>
    public Guid? InvitedByUserId { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public User? InvitedByUser { get; set; }
    
    // Domain methods for role-based permissions within this organization
    
    /// <summary>
    /// Check if user is an admin in this organization
    /// </summary>
    public bool IsAdmin() => Role == UserRole.OrganizationAdmin;
    
    /// <summary>
    /// Check if user can manage other users in this organization
    /// </summary>
    public bool CanManageUsers() => Role == UserRole.OrganizationAdmin;
    
    /// <summary>
    /// Check if user can manage transcriptions in this organization
    /// </summary>
    public bool CanManageTranscriptions() => Role == UserRole.OrganizationAdmin || Role == UserRole.OrganizationUser;
    
    /// <summary>
    /// Check if user can view transcriptions in this organization
    /// </summary>
    public bool CanViewTranscriptions() => IsActive && (Role == UserRole.OrganizationAdmin || 
                                                       Role == UserRole.OrganizationUser || 
                                                       Role == UserRole.ReadOnlyUser);
    
    /// <summary>
    /// Check if user can export transcriptions from this organization
    /// </summary>
    public bool CanExportTranscriptions() => Role == UserRole.OrganizationAdmin || Role == UserRole.OrganizationUser;
    
    // User lifecycle methods for this organization
    
    /// <summary>
    /// Update the user's role in this organization
    /// </summary>
    public void UpdateRole(UserRole newRole)
    {
        Role = newRole;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Deactivate user in this organization
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Activate user in this organization
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Accept invitation to join this organization
    /// </summary>
    public void AcceptInvitation()
    {
        InvitationAcceptedAt = DateTime.UtcNow;
        InvitationToken = null;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // Validation methods that throw domain exceptions
    
    /// <summary>
    /// Validate that user can manage other users in this organization
    /// </summary>
    public void ValidateCanManageUsers()
    {
        if (!CanManageUsers())
        {
            throw new UserPermissionException($"User does not have permission to manage users in organization {OrganizationId}.");
        }
    }
    
    /// <summary>
    /// Validate that user can manage transcriptions in this organization
    /// </summary>
    public void ValidateCanManageTranscriptions()
    {
        if (!CanManageTranscriptions())
        {
            throw new UserPermissionException($"User does not have permission to manage transcriptions in organization {OrganizationId}.");
        }
    }
    
    /// <summary>
    /// Validate that user can view transcriptions in this organization
    /// </summary>
    public void ValidateCanViewTranscriptions()
    {
        if (!CanViewTranscriptions())
        {
            throw new UserPermissionException($"User does not have permission to view transcriptions in organization {OrganizationId}.");
        }
    }
    
    /// <summary>
    /// Validate that user can export transcriptions from this organization
    /// </summary>
    public void ValidateCanExportTranscriptions()
    {
        if (!CanExportTranscriptions())
        {
            throw new UserPermissionException($"User does not have permission to export transcriptions in organization {OrganizationId}.");
        }
    }
    
    /// <summary>
    /// Validate that user is active in this organization
    /// </summary>
    public void ValidateIsActive()
    {
        if (!IsActive)
        {
            throw new UserAuthenticationException($"User is not active in organization {OrganizationId}.");
        }
    }
} 