using Microsoft.AspNetCore.Authorization;
using SermonTranscription.Domain.Enums;

namespace SermonTranscription.Api.Authorization;

/// <summary>
/// Defines authorization policies for role-based access control
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Policy for organization administrators - full access to organization resources
    /// </summary>
    public const string OrganizationAdmin = "OrganizationAdmin";
    
    /// <summary>
    /// Policy for organization users - can manage transcriptions and view data
    /// </summary>
    public const string OrganizationUser = "OrganizationUser";
    
    /// <summary>
    /// Policy for read-only users - can only view transcriptions
    /// </summary>
    public const string ReadOnlyUser = "ReadOnlyUser";
    
    /// <summary>
    /// Policy for users who can manage other users in an organization
    /// </summary>
    public const string CanManageUsers = "CanManageUsers";
    
    /// <summary>
    /// Policy for users who can manage transcriptions in an organization
    /// </summary>
    public const string CanManageTranscriptions = "CanManageTranscriptions";
    
    /// <summary>
    /// Policy for users who can view transcriptions in an organization
    /// </summary>
    public const string CanViewTranscriptions = "CanViewTranscriptions";
    
    /// <summary>
    /// Policy for users who can export transcriptions from an organization
    /// </summary>
    public const string CanExportTranscriptions = "CanExportTranscriptions";
    
    /// <summary>
    /// Policy for users who are active members of an organization
    /// </summary>
    public const string OrganizationMember = "OrganizationMember";
    
    /// <summary>
    /// Policy for users who are authenticated and have a valid JWT token
    /// </summary>
    public const string AuthenticatedUser = "AuthenticatedUser";
} 