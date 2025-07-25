using FluentValidation;
using SermonTranscription.Application.DTOs;

namespace SermonTranscription.Application.Validators;

public class UpdateOrganizationSettingsRequestValidator : AbstractValidator<UpdateOrganizationSettingsRequest>
{
    public UpdateOrganizationSettingsRequestValidator()
    {
        RuleFor(x => x.MaxTranscriptionMinutes)
            .InclusiveBetween(1, 100000).WithMessage("Max transcription minutes must be between 1 and 100000")
            .When(x => x.MaxTranscriptionMinutes.HasValue);
    }
}
