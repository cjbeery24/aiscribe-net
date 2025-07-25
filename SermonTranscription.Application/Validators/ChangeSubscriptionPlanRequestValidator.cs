using FluentValidation;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Domain.Enums;

namespace SermonTranscription.Application.Validators;

public class ChangeSubscriptionPlanRequestValidator : AbstractValidator<ChangeSubscriptionPlanRequest>
{
    public ChangeSubscriptionPlanRequestValidator()
    {
        RuleFor(x => x.NewPlan)
            .IsInEnum().WithMessage("Invalid subscription plan");
    }
}
