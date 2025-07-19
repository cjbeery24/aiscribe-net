using System.ComponentModel.DataAnnotations;
using SermonTranscription.Domain.Interfaces;

namespace SermonTranscription.Domain.Entities;

public class Transcription : IMultiTenantEntity
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    // Metadata
    public string Language { get; set; } = "en";
    public bool HasSpeakerDiarization { get; set; }
    public bool HasTimestamps { get; set; }
    public bool HasPunctuation { get; set; }

    // Audio information
    public int? DurationSeconds { get; set; }
    public long? AudioFileSizeBytes { get; set; }
    public string? AudioFileName { get; set; }

    // Processing information
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

    // Search and filtering
    public string[]? Tags { get; set; }
    public string? Speaker { get; set; }
    public DateTime? EventDate { get; set; }

    // Export options
    public bool IsPublic { get; set; } = false;
    public string? ExportUrl { get; set; }
    public DateTime? ExportedAt { get; set; }

    // Navigation properties
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public Guid? SessionId { get; set; }
    public TranscriptionSession? Session { get; set; }

    public ICollection<TranscriptionSegment> Segments { get; set; } = new List<TranscriptionSegment>();

    // Computed properties
    public string ContentPreview => Content.Length > 200 ? Content[..200] + "..." : Content;
    public TimeSpan? Duration => DurationSeconds.HasValue ? TimeSpan.FromSeconds(DurationSeconds.Value) : null;

    // Domain methods
    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return;

        var currentTags = Tags?.ToList() ?? new List<string>();
        if (!currentTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
        {
            currentTags.Add(tag);
            Tags = currentTags.ToArray();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RemoveTag(string tag)
    {
        if (Tags == null || string.IsNullOrWhiteSpace(tag)) return;

        var currentTags = Tags.ToList();
        if (currentTags.RemoveAll(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)) > 0)
        {
            Tags = currentTags.ToArray();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void MarkAsProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Export(string url)
    {
        ExportUrl = url;
        ExportedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

public class TranscriptionSegment
{
    public Guid Id { get; set; }

    [Required]
    public string Text { get; set; } = string.Empty;

    public double StartTime { get; set; }
    public double EndTime { get; set; }

    public string? Speaker { get; set; }
    public double Confidence { get; set; }

    public int SequenceNumber { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Guid TranscriptionId { get; set; }
    public Transcription Transcription { get; set; } = null!;

    // Computed properties
    public TimeSpan StartTimeSpan => TimeSpan.FromSeconds(StartTime);
    public TimeSpan EndTimeSpan => TimeSpan.FromSeconds(EndTime);
    public TimeSpan Duration => EndTimeSpan - StartTimeSpan;

    public string FormattedTimestamp => $"{StartTimeSpan:mm\\:ss} - {EndTimeSpan:mm\\:ss}";
}
