using SermonTranscription.Domain.Entities;

namespace SermonTranscription.Application.DTOs;

public class TranscriptionSessionResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SessionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Audio processing
    public string? AudioStreamUrl { get; set; }
    public string? AudioFileName { get; set; }
    public long? AudioFileSizeBytes { get; set; }
    public int? AudioDurationSeconds { get; set; }

    // Transcription settings
    public string Language { get; set; } = string.Empty;
    public bool EnableSpeakerDiarization { get; set; }
    public bool EnablePunctuation { get; set; }
    public bool EnableTimestamps { get; set; }

    // Real-time processing
    public string? GladiaSessionId { get; set; }
    public string? WebSocketConnectionId { get; set; }
    public bool IsLive { get; set; }

    // Error handling
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime? LastRetryAt { get; set; }

    // Organization and user info
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;

    // Computed properties
    public TimeSpan? Duration { get; set; }
    public bool IsActive { get; set; }
    public bool CanStart { get; set; }
    public bool CanPause { get; set; }
    public bool CanResume { get; set; }
    public bool CanComplete { get; set; }

    // Statistics
    public int TranscriptionCount { get; set; }
    public TimeSpan? TotalTranscriptionDuration { get; set; }
}
