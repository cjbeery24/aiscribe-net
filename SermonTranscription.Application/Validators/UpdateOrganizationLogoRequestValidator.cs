using FluentValidation;
using SermonTranscription.Application.DTOs;

namespace SermonTranscription.Application.Validators;

public class UpdateOrganizationLogoRequestValidator : AbstractValidator<UpdateOrganizationLogoRequest>
{
    public UpdateOrganizationLogoRequestValidator()
    {
        RuleFor(x => x.LogoUrl)
            .NotEmpty().WithMessage("Logo URL is required")
            .Must(BeAValidUrl).WithMessage("Invalid logo URL");
    }

    private static bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }
}
