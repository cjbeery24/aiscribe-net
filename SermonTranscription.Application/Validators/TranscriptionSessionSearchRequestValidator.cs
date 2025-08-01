using FluentValidation;
using SermonTranscription.Application.DTOs;

namespace SermonTranscription.Application.Validators;

public class TranscriptionSessionSearchRequestValidator : AbstractValidator<TranscriptionSessionSearchRequest>
{
    public TranscriptionSessionSearchRequestValidator()
    {
        RuleFor(x => x.SearchTerm)
            .MaximumLength(200).WithMessage("Search term cannot exceed 200 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.SearchTerm));

        RuleFor(x => x.Language)
            .MaximumLength(10).WithMessage("Language code cannot exceed 10 characters")
            .Matches(@"^[a-z]{2}(-[A-Z]{2})?$").WithMessage("Language must be in ISO 639-1 format (e.g., 'en' or 'en-US')")
            .When(x => !string.IsNullOrWhiteSpace(x.Language));

        RuleFor(x => x.StartDate)
            .LessThanOrEqualTo(x => x.EndDate).WithMessage("Start date must be less than or equal to end date")
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("End date must be greater than or equal to start date")
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);

        RuleFor(x => x.SortBy)
            .Must(BeValidSortField).WithMessage("Sort field must be one of: CreatedAt, Title, Status, StartedAt, CompletedAt")
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy));

        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100");
    }

    private static bool BeValidSortField(string sortBy)
    {
        var validFields = new[] { "CreatedAt", "Title", "Status", "StartedAt", "CompletedAt" };
        return validFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase);
    }
}
