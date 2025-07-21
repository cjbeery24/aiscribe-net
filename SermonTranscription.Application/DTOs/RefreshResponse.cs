namespace SermonTranscription.Application.DTOs;

/// <summary>
/// Refresh token response DTO
/// </summary>
public class RefreshResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}
