using FluentValidation;
using SermonTranscription.Application.DTOs;

namespace SermonTranscription.Application.Validators;

/// <summary>
/// Validator for user organizations request
/// </summary>
public class UserOrganizationsRequestValidator : AbstractValidator<UserOrganizationsRequest>
{
    public UserOrganizationsRequestValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100");

        RuleFor(x => x.SortBy)
            .Must(BeValidSortField)
            .When(x => !string.IsNullOrEmpty(x.SortBy))
            .WithMessage("SortBy must be one of: Name, CreatedAt, UpdatedAt, IsActive");

        RuleFor(x => x.Role)
            .Must(BeValidRole)
            .When(x => !string.IsNullOrEmpty(x.Role))
            .WithMessage("Role must be one of: OrganizationAdmin, OrganizationUser, ReadOnlyUser");
    }

    private static bool BeValidSortField(string? sortBy)
    {
        if (string.IsNullOrEmpty(sortBy)) return true;

        var validFields = new[] { "Name", "CreatedAt", "UpdatedAt", "IsActive" };
        return validFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase);
    }

    private static bool BeValidRole(string? role)
    {
        if (string.IsNullOrEmpty(role)) return true;

        var validRoles = new[] { "OrganizationAdmin", "OrganizationUser", "ReadOnlyUser" };
        return validRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}
