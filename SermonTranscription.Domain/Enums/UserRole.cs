namespace SermonTranscription.Domain.Enums;

/// <summary>
/// Defines the roles a user can have within an organization
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Anonymous user with read-only access to public sessions
    /// </summary>
    Anonymous = 0,

    /// <summary>
    /// Organization administrator with full control over the account
    /// </summary>
    OrganizationAdmin = 1,

    /// <summary>
    /// Regular organization user with limited permissions
    /// </summary>
    OrganizationUser = 2,

    /// <summary>
    /// Read-only user with minimal permissions
    /// </summary>
    ReadOnlyUser = 3
}
