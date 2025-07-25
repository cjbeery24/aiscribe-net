using FluentValidation;
using SermonTranscription.Application.DTOs;

namespace SermonTranscription.Application.Validators;

public class OrganizationUserSearchRequestValidator : AbstractValidator<OrganizationUserSearchRequest>
{
    public OrganizationUserSearchRequestValidator()
    {
        RuleFor(x => x.SearchTerm)
            .MaximumLength(200).WithMessage("Search term cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.SearchTerm));

        RuleFor(x => x.Role)
            .Must(BeValidRole).WithMessage("Invalid role")
            .When(x => !string.IsNullOrEmpty(x.Role));

        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100");

        RuleFor(x => x.SortBy)
            .Must(BeValidSortField).WithMessage("Invalid sort field")
            .When(x => !string.IsNullOrEmpty(x.SortBy));
    }

    private static bool BeValidRole(string? role)
    {
        var validRoles = new[] { "OrganizationUser", "OrganizationAdmin", "OrganizationOwner" };
        return validRoles.Contains(role);
    }

    private static bool BeValidSortField(string? sortBy)
    {
        var validSortFields = new[] { "FirstName", "LastName", "Email", "Role", "CreatedAt", "IsActive", "IsEmailVerified" };
        return validSortFields.Contains(sortBy);
    }
}
