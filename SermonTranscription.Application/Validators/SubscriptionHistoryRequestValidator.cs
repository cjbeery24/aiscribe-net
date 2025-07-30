using FluentValidation;
using SermonTranscription.Application.DTOs;

namespace SermonTranscription.Application.Validators;

/// <summary>
/// Validator for subscription history request
/// </summary>
public class SubscriptionHistoryRequestValidator : AbstractValidator<SubscriptionHistoryRequest>
{
    public SubscriptionHistoryRequestValidator()
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
            .WithMessage("SortBy must be one of: CreatedAt, UpdatedAt, Plan, Status, Price");

        RuleFor(x => x.Status)
            .Must(BeValidStatus)
            .When(x => !string.IsNullOrEmpty(x.Status))
            .WithMessage("Status must be one of: Active, Cancelled, Suspended, PastDue, Expired");

        RuleFor(x => x.Plan)
            .Must(BeValidPlan)
            .When(x => !string.IsNullOrEmpty(x.Plan))
            .WithMessage("Plan must be one of: Basic, Professional, Enterprise");
    }

    private static bool BeValidSortField(string? sortBy)
    {
        if (string.IsNullOrEmpty(sortBy)) return true;

        var validFields = new[] { "CreatedAt", "UpdatedAt", "Plan", "Status", "Price" };
        return validFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase);
    }

    private static bool BeValidStatus(string? status)
    {
        if (string.IsNullOrEmpty(status)) return true;

        var validStatuses = new[] { "Active", "Cancelled", "Suspended", "PastDue", "Expired" };
        return validStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);
    }

    private static bool BeValidPlan(string? plan)
    {
        if (string.IsNullOrEmpty(plan)) return true;

        var validPlans = new[] { "Basic", "Professional", "Enterprise" };
        return validPlans.Contains(plan, StringComparer.OrdinalIgnoreCase);
    }
}
