using EcoNexus.Domain.Enums;
using FluentValidation;

namespace EcoNexus.Application.Features.Recycling.RecordIntake;

internal sealed class RecordIntakeValidator : AbstractValidator<RecordIntakeCommand>
{
    public RecordIntakeValidator()
    {
        RuleFor(x => x.FacilityId)
            .NotEmpty();

        RuleFor(x => x.Request).NotNull();

        When(x => x.Request is not null, () =>
        {
            RuleFor(x => x.Request.Material)
                .NotEmpty()
                .Must(BeAValidMaterial)
                    .WithMessage("Material must be a valid WasteCategory name.");

            RuleFor(x => x.Request.WeightKilograms)
                .GreaterThan(0);
        });
    }

    private static bool BeAValidMaterial(string value)
        => Enum.TryParse<WasteCategory>(value, ignoreCase: true, out _);
}
