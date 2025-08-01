namespace SermonTranscription.Application.DTOs;

public class CreateTranscriptionSessionRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Language { get; set; } = "en";
    public bool EnableSpeakerDiarization { get; set; } = true;
    public bool EnablePunctuation { get; set; } = true;
    public bool EnableTimestamps { get; set; } = true;
    public string? AudioStreamUrl { get; set; }
    public string? AudioFileName { get; set; }
    public bool IsLive { get; set; } = false;
}
