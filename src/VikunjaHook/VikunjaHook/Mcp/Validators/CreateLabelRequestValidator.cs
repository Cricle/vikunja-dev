using FluentValidation;
using VikunjaHook.Mcp.Models.Requests;

namespace VikunjaHook.Mcp.Validators;

/// <summary>
/// Validator for CreateLabelRequest
/// </summary>
public class CreateLabelRequestValidator : AbstractValidator<CreateLabelRequest>
{
    public CreateLabelRequestValidator()
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
