using EcoNexus.Domain.Enums;
using FluentValidation;

namespace EcoNexus.Application.Features.Recycling.AdvanceIntake;

internal sealed class AdvanceIntakeValidator : AbstractValidator<AdvanceIntakeCommand>
{
    public AdvanceIntakeValidator()
    {
        RuleFor(x => x.FacilityId)
            .NotEmpty();

        RuleFor(x => x.IntakeId)
            .NotEmpty();

        RuleFor(x => x.Request).NotNull();

        When(x => x.Request is not null, () =>
        {
            RuleFor(x => x.Request.NextStage)
                .NotEmpty()
                .Must(BeAValidStage)
                    .WithMessage("NextStage must be a valid IntakeStage name.");
        });
    }

    private static bool BeAValidStage(string value)
        => Enum.TryParse<IntakeStage>(value, ignoreCase: true, out _);
}
