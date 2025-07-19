using Microsoft.AspNetCore.Authorization;
using SermonTranscription.Api.Authorization;

namespace SermonTranscription.Api.Authorization;

/// <summary>
/// Authorization attribute for organization administrators
/// </summary>
public class RequireOrganizationAdminAttribute : AuthorizeAttribute
{
    public RequireOrganizationAdminAttribute() : base(AuthorizationPolicies.OrganizationAdmin)
    {
    }
}

/// <summary>
/// Authorization attribute for organization users (admin or regular user)
/// </summary>
public class RequireOrganizationUserAttribute : AuthorizeAttribute
{
    public RequireOrganizationUserAttribute() : base(AuthorizationPolicies.OrganizationUser)
    {
    }
}

/// <summary>
/// Authorization attribute for read-only users
/// </summary>
public class RequireReadOnlyUserAttribute : AuthorizeAttribute
{
    public RequireReadOnlyUserAttribute() : base(AuthorizationPolicies.ReadOnlyUser)
    {
    }
}

/// <summary>
/// Authorization attribute for users who can manage other users
/// </summary>
public class RequireCanManageUsersAttribute : AuthorizeAttribute
{
    public RequireCanManageUsersAttribute() : base(AuthorizationPolicies.CanManageUsers)
    {
    }
}

/// <summary>
/// Authorization attribute for users who can manage transcriptions
/// </summary>
public class RequireCanManageTranscriptionsAttribute : AuthorizeAttribute
{
    public RequireCanManageTranscriptionsAttribute() : base(AuthorizationPolicies.CanManageTranscriptions)
    {
    }
}

/// <summary>
/// Authorization attribute for users who can view transcriptions
/// </summary>
public class RequireCanViewTranscriptionsAttribute : AuthorizeAttribute
{
    public RequireCanViewTranscriptionsAttribute() : base(AuthorizationPolicies.CanViewTranscriptions)
    {
    }
}

/// <summary>
/// Authorization attribute for users who can export transcriptions
/// </summary>
public class RequireCanExportTranscriptionsAttribute : AuthorizeAttribute
{
    public RequireCanExportTranscriptionsAttribute() : base(AuthorizationPolicies.CanExportTranscriptions)
    {
    }
}

/// <summary>
/// Authorization attribute for organization members (any active member)
/// </summary>
public class RequireOrganizationMemberAttribute : AuthorizeAttribute
{
    public RequireOrganizationMemberAttribute() : base(AuthorizationPolicies.OrganizationMember)
    {
    }
}

/// <summary>
/// Authorization attribute for authenticated users (any valid JWT token)
/// </summary>
public class RequireAuthenticatedUserAttribute : AuthorizeAttribute
{
    public RequireAuthenticatedUserAttribute() : base(AuthorizationPolicies.AuthenticatedUser)
    {
    }
} 