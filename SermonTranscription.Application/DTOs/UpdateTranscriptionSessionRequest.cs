namespace SermonTranscription.Application.DTOs;

public class UpdateTranscriptionSessionRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Language { get; set; }
    public bool? EnableSpeakerDiarization { get; set; }
    public bool? EnablePunctuation { get; set; }
    public bool? EnableTimestamps { get; set; }
    public string? AudioStreamUrl { get; set; }
    public string? AudioFileName { get; set; }
}
