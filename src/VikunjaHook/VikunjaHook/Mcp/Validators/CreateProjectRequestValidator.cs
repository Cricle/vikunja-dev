using FluentValidation;
using VikunjaHook.Mcp.Models.Requests;

namespace VikunjaHook.Mcp.Validators;

/// <summary>
/// Validator for CreateProjectRequest
/// </summary>
public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .MaximumLength(250)
            .WithMessage("Title must not exceed 250 characters");

        RuleFor(x => x.HexColor)
            .Must(BeValidHexColor)
            .When(x => !string.IsNullOrEmpty(x.HexColor))
            .WithMessage("Hex color must be in format #RRGGBB");

        RuleFor(x => x.ParentProjectId)
            .GreaterThan(0)
            .When(x => x.ParentProjectId.HasValue)
            .WithMessage("Parent project ID must be greater than 0");
    }

    private static bool BeValidHexColor(string? hexColor)
    {
        if (string.IsNullOrEmpty(hexColor))
            return true;

        return System.Text.RegularExpressions.Regex.IsMatch(
            hexColor, 
            @"^#[0-9A-Fa-f]{6}$");
    }
}
