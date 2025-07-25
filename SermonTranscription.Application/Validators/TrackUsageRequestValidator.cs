using FluentValidation;
using SermonTranscription.Application.DTOs;

namespace SermonTranscription.Application.Validators;

public class TrackUsageRequestValidator : AbstractValidator<TrackUsageRequest>
{
    public TrackUsageRequestValidator()
    {
        RuleFor(x => x.MinutesUsed)
            .GreaterThan(0).WithMessage("Minutes used must be greater than 0");
    }
}
