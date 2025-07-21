namespace SermonTranscription.Application.DTOs;

public class RefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
}
