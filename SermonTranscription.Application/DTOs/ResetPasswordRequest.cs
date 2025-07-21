namespace SermonTranscription.Application.DTOs;

/// <summary>
/// Reset password request DTO
/// </summary>
public class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
