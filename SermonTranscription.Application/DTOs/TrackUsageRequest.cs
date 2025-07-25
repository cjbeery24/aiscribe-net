using System.ComponentModel.DataAnnotations;

namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for tracking transcription usage
/// </summary>
public class TrackUsageRequest
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Minutes used must be greater than 0")]
    public int MinutesUsed { get; set; }
}
