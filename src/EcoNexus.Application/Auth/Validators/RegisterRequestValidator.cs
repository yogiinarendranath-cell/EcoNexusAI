using EcoNexus.Contracts.Auth;
using FluentValidation;

namespace EcoNexus.Application.Auth.Validators;

/// <summary>
/// Validates RegisterRequest before it reaches the controller.
/// Password policy mirrors ASP.NET Core Identity's policy in Program.cs —
/// this provides a friendlier error message than Identity's own.
/// </summary>
public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(256).WithMessage("Email must be 256 characters or fewer.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(128).WithMessage("Password must be 128 characters or fewer.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one non-alphanumeric character.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MaximumLength(100).WithMessage("Display name must be 100 characters or fewer.");
    }
}
