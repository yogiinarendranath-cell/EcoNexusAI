using EcoNexus.Contracts.Auth;
using FluentValidation;

namespace EcoNexus.Application.Auth.Validators;

/// <summary>
/// Validates RefreshTokenRequest for the /refresh and /logout endpoints.
/// The token is a base64url-encoded opaque string — just ensure it's present.
/// </summary>
public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.")
            .MaximumLength(512).WithMessage("Refresh token is too long.");
    }
}
