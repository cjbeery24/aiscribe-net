using FluentValidation;
using SermonTranscription.Application.DTOs;

namespace SermonTranscription.Application.Validators;

public class UpdateOrganizationUserRoleRequestValidator : AbstractValidator<UpdateOrganizationUserRoleRequest>
{
    public UpdateOrganizationUserRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required")
            .Must(BeValidRole).WithMessage("Invalid role specified");
    }

    private static bool BeValidRole(string role)
    {
        var validRoles = new[] { "OrganizationUser", "OrganizationAdmin", "OrganizationOwner" };
        return validRoles.Contains(role);
    }
}
