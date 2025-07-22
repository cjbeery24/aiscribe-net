namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for organization response that includes user information
/// </summary>
public class OrganizationWithUsersResponse : OrganizationResponse
{
    /// <summary>
    /// List of users in the organization
    /// </summary>
    public List<OrganizationUserResponse> Users { get; set; } = new();
}
