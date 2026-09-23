using EcoNexus.Domain.Enums;
using FluentValidation;

namespace EcoNexus.Application.Features.Stations.UpdateStation;

public sealed class UpdateStationValidator : AbstractValidator<UpdateStationCommand>
{
    public UpdateStationValidator()
    {
        RuleFor(x => x.StationId).NotEmpty();

        RuleFor(x => x.Request.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Request.Longitude).InclusiveBetween(-180, 180);

        RuleFor(x => x.Request.CapacityKilograms)
            .GreaterThan(0)
            .LessThanOrEqualTo(10_000);

        RuleFor(x => x.Request.PrimaryCategory)
            .NotEmpty()
            .Must(BeAValidWasteCategory)
            .WithMessage("PrimaryCategory must be a valid WasteCategory name.");
    }

    private static bool BeAValidWasteCategory(string value)
        => Enum.TryParse<WasteCategory>(value, ignoreCase: true, out _);
}
