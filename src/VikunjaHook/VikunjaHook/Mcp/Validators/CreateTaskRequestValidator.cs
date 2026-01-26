using FluentValidation;
using VikunjaHook.Mcp.Models.Requests;

namespace VikunjaHook.Mcp.Validators;

/// <summary>
/// Validator for CreateTaskRequest
/// </summary>
public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.ProjectId)
            .GreaterThan(0)
            .WithMessage("Project ID must be greater than 0");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .MaximumLength(250)
            .WithMessage("Title must not exceed 250 characters");

        RuleFor(x => x.Priority)
            .InclusiveBetween(1, 5)
            .When(x => x.Priority.HasValue)
            .WithMessage("Priority must be between 1 and 5");

        RuleFor(x => x.DueDate)
            .Must(BeValidIso8601Date)
            .When(x => x.DueDate.HasValue)
            .WithMessage("Due date must be a valid UTC date");

        RuleFor(x => x.RepeatMode)
            .Must(x => x == null || new[] { "day", "week", "month", "year" }.Contains(x))
            .WithMessage("Repeat mode must be one of: day, week, month, year");

        RuleFor(x => x.RepeatAfter)
            .GreaterThan(0)
            .When(x => x.RepeatAfter.HasValue)
            .WithMessage("Repeat after must be greater than 0");
    }

    private static bool BeValidIso8601Date(DateTime? date)
    {
        return date == null || date.Value.Kind == DateTimeKind.Utc;
    }
}
