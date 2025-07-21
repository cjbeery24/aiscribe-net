namespace SermonTranscription.Application.DTOs;

/// <summary>
/// Login response DTO
/// </summary>
public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public AuthUserInfo User { get; set; } = new();
}
