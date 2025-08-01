using FluentValidation;
using SermonTranscription.Application.DTOs;

namespace SermonTranscription.Application.Validators;

public class CreateTranscriptionSessionRequestValidator : AbstractValidator<CreateTranscriptionSessionRequest>
{
    public CreateTranscriptionSessionRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters")
            .MinimumLength(1).WithMessage("Title must be at least 1 character");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Language)
            .NotEmpty().WithMessage("Language is required")
            .MaximumLength(10).WithMessage("Language code cannot exceed 10 characters")
            .Matches(@"^[a-z]{2}(-[A-Z]{2})?$").WithMessage("Language must be in ISO 639-1 format (e.g., 'en' or 'en-US')");

        RuleFor(x => x.AudioStreamUrl)
            .MaximumLength(500).WithMessage("Audio stream URL cannot exceed 500 characters")
            .Must(BeValidUrl).WithMessage("Audio stream URL must be a valid URL")
            .When(x => !string.IsNullOrWhiteSpace(x.AudioStreamUrl));

        RuleFor(x => x.AudioFileName)
            .MaximumLength(255).WithMessage("Audio file name cannot exceed 255 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.AudioFileName));
    }

    private static bool BeValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }
}
