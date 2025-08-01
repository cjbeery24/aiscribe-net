using System.ComponentModel.DataAnnotations;

namespace SermonTranscription.Application.DTOs;

/// <summary>
/// Request to start audio streaming for a session
/// </summary>
public class StartAudioStreamRequest
{
    /// <summary>
    /// The transcription session ID
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Audio format (wav, mp3, m4a, flac)
    /// </summary>
    public string AudioFormat { get; set; } = "wav";

    /// <summary>
    /// Sample rate in Hz
    /// </summary>
    public int SampleRate { get; set; } = 16000;

    /// <summary>
    /// Number of audio channels (1 for mono, 2 for stereo)
    /// </summary>
    public int Channels { get; set; } = 1;

    /// <summary>
    /// Whether to use WebSocket streaming (true) or chunked HTTP upload (false)
    /// </summary>
    public bool UseWebSocket { get; set; } = true;
}

/// <summary>
/// Response for audio stream status
/// </summary>
public class AudioStreamStatusResponse
{
    /// <summary>
    /// The transcription session ID
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Whether the session is currently active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Current session status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Whether the session can receive audio
    /// </summary>
    public bool CanReceiveAudio { get; set; }

    /// <summary>
    /// Last activity timestamp
    /// </summary>
    public DateTime LastActivityAt { get; set; }

    /// <summary>
    /// Whether WebSocket streaming is supported
    /// </summary>
    public bool SupportsWebSocket { get; set; }

    /// <summary>
    /// Whether chunked HTTP upload is supported
    /// </summary>
    public bool SupportsChunkedUpload { get; set; }

    /// <summary>
    /// Maximum chunk size in bytes
    /// </summary>
    public int MaxChunkSizeBytes { get; set; }

    /// <summary>
    /// Supported audio formats
    /// </summary>
    public string[] SupportedAudioFormats { get; set; } = Array.Empty<string>();

    /// <summary>
    /// WebSocket endpoint URL (if supported)
    /// </summary>
    public string? WebSocketUrl { get; set; }

    /// <summary>
    /// HTTP upload endpoint URL (if supported)
    /// </summary>
    public string? UploadUrl { get; set; }
}

/// <summary>
/// Audio chunk upload request
/// </summary>
public class AudioChunkRequest
{
    /// <summary>
    /// Sequential chunk index for ordering
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// Whether this is the final audio chunk
    /// </summary>
    public bool IsFinalChunk { get; set; }

    /// <summary>
    /// Audio data as base64 string (for JSON requests)
    /// </summary>
    public string? AudioDataBase64 { get; set; }

    /// <summary>
    /// Audio format
    /// </summary>
    public string AudioFormat { get; set; } = "wav";

    /// <summary>
    /// Timestamp when the chunk was captured
    /// </summary>
    public DateTime? CapturedAt { get; set; }
}

/// <summary>
/// Audio chunk response
/// </summary>
public class AudioChunkResponse
{
    /// <summary>
    /// The chunk index that was processed
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// Whether the chunk was successfully processed
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Size of the processed chunk in bytes
    /// </summary>
    public int SizeBytes { get; set; }

    /// <summary>
    /// Processing timestamp
    /// </summary>
    public DateTime ProcessedAt { get; set; }

    /// <summary>
    /// Error message if processing failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// WebSocket audio stream message
/// </summary>
public class WebSocketAudioMessage
{
    /// <summary>
    /// Message type
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Session ID
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Message data
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Audio stream configuration
/// </summary>
public class AudioStreamConfiguration
{
    /// <summary>
    /// Maximum chunk size in bytes
    /// </summary>
    public int MaxChunkSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// Maximum session duration
    /// </summary>
    public TimeSpan MaxSessionDuration { get; set; } = TimeSpan.FromHours(4);

    /// <summary>
    /// Supported audio formats
    /// </summary>
    public string[] SupportedFormats { get; set; } = { "wav", "mp3", "m4a", "flac" };

    /// <summary>
    /// Supported sample rates
    /// </summary>
    public int[] SupportedSampleRates { get; set; } = { 8000, 16000, 22050, 44100, 48000 };

    /// <summary>
    /// Whether to enable real-time transcription
    /// </summary>
    public bool EnableRealTimeTranscription { get; set; } = true;

    /// <summary>
    /// Whether to enable audio buffering
    /// </summary>
    public bool EnableAudioBuffering { get; set; } = true;

    /// <summary>
    /// Buffer size in seconds
    /// </summary>
    public int BufferSizeSeconds { get; set; } = 5;
}
