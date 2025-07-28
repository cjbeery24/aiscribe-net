using FluentValidation;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Domain.Enums;

namespace SermonTranscription.Application.Validators;

public class CreateSubscriptionRequestValidator : AbstractValidator<CreateSubscriptionRequest>
{
    public CreateSubscriptionRequestValidator()
    {
        RuleFor(x => x.Plan)
            .IsInEnum().WithMessage("Invalid subscription plan");
    }
}
