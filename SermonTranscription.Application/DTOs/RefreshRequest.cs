namespace SermonTranscription.Application.DTOs;

/// <summary>
/// Refresh token request DTO
/// </summary>
public class RefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
