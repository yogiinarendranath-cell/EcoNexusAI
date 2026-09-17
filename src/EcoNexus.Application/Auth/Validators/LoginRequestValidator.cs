using EcoNexus.Contracts.Auth;
using FluentValidation;

namespace EcoNexus.Application.Auth.Validators;

/// <summary>
/// Validates LoginRequest. Kept intentionally minimal — we don't reveal whether
/// an email exists or a password matches here; that's decided by the sign-in
/// manager in the controller.
/// </summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
