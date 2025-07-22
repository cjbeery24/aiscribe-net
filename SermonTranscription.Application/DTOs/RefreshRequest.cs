using System.ComponentModel.DataAnnotations;

namespace SermonTranscription.Application.DTOs;

public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;

    [Required]
    public string AccessToken { get; set; } = string.Empty;
}
