using FluentValidation;
using VikunjaHook.Mcp.Models.Requests;

namespace VikunjaHook.Mcp.Validators;

/// <summary>
/// Validator for UpdateTaskRequest
/// </summary>
public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Task ID must be greater than 0");

        RuleFor(x => x.Title)
            .MaximumLength(250)
            .When(x => !string.IsNullOrEmpty(x.Title))
            .WithMessage("Title must not exceed 250 characters");

        RuleFor(x => x.Priority)
            .InclusiveBetween(1, 5)
            .When(x => x.Priority.HasValue)
            .WithMessage("Priority must be between 1 and 5");

        RuleFor(x => x.DueDate)
            .Must(BeValidIso8601Date)
            .When(x => x.DueDate.HasValue)
            .WithMessage("Due date must be a valid UTC date");
    }

    private static bool BeValidIso8601Date(DateTime? date)
    {
        return date == null || date.Value.Kind == DateTimeKind.Utc;
    }
}
