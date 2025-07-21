namespace SermonTranscription.Application.DTOs;

/// <summary>
/// Forgot password request DTO
/// </summary>
public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}
