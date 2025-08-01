using FluentValidation;
using SermonTranscription.Application.DTOs;

namespace SermonTranscription.Application.Validators;

/// <summary>
/// Validator for AudioChunkRequest
/// </summary>
public class AudioChunkRequestValidator : AbstractValidator<AudioChunkRequest>
{
    public AudioChunkRequestValidator()
    {
        RuleFor(x => x.ChunkIndex)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Chunk index must be non-negative");

        RuleFor(x => x.AudioFormat)
            .NotEmpty()
            .WithMessage("Audio format is required")
            .Must(BeValidAudioFormat)
            .WithMessage("Audio format must be one of: wav, mp3, m4a, flac");

        RuleFor(x => x.AudioDataBase64)
            .NotEmpty()
            .When(x => string.IsNullOrEmpty(x.AudioDataBase64))
            .WithMessage("Audio data is required");

        RuleFor(x => x.CapturedAt)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .When(x => x.CapturedAt.HasValue)
            .WithMessage("Captured timestamp cannot be in the future");
    }

    private static bool BeValidAudioFormat(string format)
    {
        var validFormats = new[] { "wav", "mp3", "m4a", "flac" };
        return validFormats.Contains(format.ToLowerInvariant());
    }
}
