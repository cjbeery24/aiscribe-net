using System.ComponentModel.DataAnnotations;

namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for organization settings update
/// </summary>
public class UpdateOrganizationSettingsRequest
{
    [Range(1, 100000, ErrorMessage = "Max transcription minutes must be between 1 and 100000")]
    public int? MaxTranscriptionMinutes { get; set; }

    public bool? CanExportTranscriptions { get; set; }
    public bool? HasRealtimeTranscription { get; set; }
}
