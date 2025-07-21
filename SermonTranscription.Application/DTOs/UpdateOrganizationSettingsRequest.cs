using System.ComponentModel.DataAnnotations;

namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for organization settings update
/// </summary>
public class UpdateOrganizationSettingsRequest
{
    [Range(1, 1000, ErrorMessage = "Max users must be between 1 and 1000")]
    public int? MaxUsers { get; set; }

    [Range(1, 10000, ErrorMessage = "Max transcription hours must be between 1 and 10000")]
    public int? MaxTranscriptionHours { get; set; }

    public bool? CanExportTranscriptions { get; set; }
    public bool? HasRealtimeTranscription { get; set; }
}
