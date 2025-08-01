using FluentValidation;
using SermonTranscription.Application.DTOs;

namespace SermonTranscription.Application.Validators;

/// <summary>
/// Validator for StartAudioStreamRequest
/// </summary>
public class StartAudioStreamRequestValidator : AbstractValidator<StartAudioStreamRequest>
{
    public StartAudioStreamRequestValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("Session ID is required");

        RuleFor(x => x.AudioFormat)
            .NotEmpty()
            .WithMessage("Audio format is required")
            .Must(BeValidAudioFormat)
            .WithMessage("Audio format must be one of: wav, mp3, m4a, flac");

        RuleFor(x => x.SampleRate)
            .InclusiveBetween(8000, 48000)
            .WithMessage("Sample rate must be between 8000 and 48000 Hz");

        RuleFor(x => x.Channels)
            .InclusiveBetween(1, 2)
            .WithMessage("Channels must be 1 (mono) or 2 (stereo)");
    }

    private static bool BeValidAudioFormat(string format)
    {
        var validFormats = new[] { "wav", "mp3", "m4a", "flac" };
        return validFormats.Contains(format.ToLowerInvariant());
    }
}
