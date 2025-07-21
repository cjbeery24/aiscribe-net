namespace SermonTranscription.Application.DTOs;

/// <summary>
/// Error response DTO
/// </summary>
public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string[] Errors { get; set; } = Array.Empty<string>();
}
