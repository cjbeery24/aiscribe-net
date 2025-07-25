namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for updating user profile information
/// </summary>
public class UpdateUserProfileRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
