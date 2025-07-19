using System.ComponentModel.DataAnnotations;
using SermonTranscription.Domain.Interfaces;

namespace SermonTranscription.Domain.Entities;

public enum SessionStatus
{
    Created,
    InProgress,
    Paused,
    Completed,
    Failed,
    Cancelled
}

public class TranscriptionSession : IMultiTenantEntity
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public SessionStatus Status { get; set; } = SessionStatus.Created;

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
    public string Language { get; set; } = "en";
    public bool EnableSpeakerDiarization { get; set; } = true;
    public bool EnablePunctuation { get; set; } = true;
    public bool EnableTimestamps { get; set; } = true;

    // Real-time processing
    public string? GladiaSessionId { get; set; }
    public string? WebSocketConnectionId { get; set; }
    public bool IsLive { get; set; } = false;

    // Error handling
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;
    public DateTime? LastRetryAt { get; set; }

    // Navigation properties
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public ICollection<Transcription> Transcriptions { get; set; } = new List<Transcription>();

    // Computed properties
    public TimeSpan? Duration => CompletedAt.HasValue && StartedAt.HasValue
        ? CompletedAt.Value - StartedAt.Value
        : null;

    public bool IsActive => Status == SessionStatus.InProgress || Status == SessionStatus.Paused;
    public bool CanStart => Status == SessionStatus.Created;
    public bool CanPause => Status == SessionStatus.InProgress;
    public bool CanResume => Status == SessionStatus.Paused;
    public bool CanComplete => Status == SessionStatus.InProgress || Status == SessionStatus.Paused;

    // Domain methods
    public void Start()
    {
        if (!CanStart)
            throw new InvalidOperationException($"Cannot start session in {Status} status");

        Status = SessionStatus.InProgress;
        StartedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsLive = true;
    }

    public void Pause()
    {
        if (!CanPause)
            throw new InvalidOperationException($"Cannot pause session in {Status} status");

        Status = SessionStatus.Paused;
        UpdatedAt = DateTime.UtcNow;
        IsLive = false;
    }

    public void Resume()
    {
        if (!CanResume)
            throw new InvalidOperationException($"Cannot resume session in {Status} status");

        Status = SessionStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
        IsLive = true;
    }

    public void Complete()
    {
        if (!CanComplete)
            throw new InvalidOperationException($"Cannot complete session in {Status} status");

        Status = SessionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsLive = false;
    }

    public void Fail(string errorMessage)
    {
        Status = SessionStatus.Failed;
        ErrorMessage = errorMessage;
        UpdatedAt = DateTime.UtcNow;
        IsLive = false;
    }

    public void Cancel()
    {
        Status = SessionStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
        IsLive = false;
    }

    public void IncrementRetry()
    {
        RetryCount++;
        LastRetryAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
