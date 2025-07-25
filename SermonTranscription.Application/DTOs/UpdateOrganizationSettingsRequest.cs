namespace SermonTranscription.Application.DTOs;

/// <summary>
/// DTO for organization settings update
/// </summary>
public class UpdateOrganizationSettingsRequest
{
    public int? MaxTranscriptionMinutes { get; set; }
    public bool? CanExportTranscriptions { get; set; }
    public bool? HasRealtimeTranscription { get; set; }
}
