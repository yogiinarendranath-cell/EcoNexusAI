using FluentValidation;

namespace EcoNexus.Application.Features.Stations.CreateStation;

internal sealed class CreateStationValidator : AbstractValidator<CreateStationCommand>
{
    public CreateStationValidator()
    {
        RuleFor(x => x.Request).NotNull();

        When(x => x.Request is not null, () =>
        {
            RuleFor(x => x.Request.Code)
                .NotEmpty()
                .Matches(@"^[A-Za-z]{2}-\d{3,6}$")
                    .WithMessage("Code must match format 'XX-999'.");

            RuleFor(x => x.Request.Latitude)
                .InclusiveBetween(-90, 90);

            RuleFor(x => x.Request.Longitude)
                .InclusiveBetween(-180, 180);

            RuleFor(x => x.Request.CapacityKilograms)
                .GreaterThan(0);

            RuleFor(x => x.Request.PrimaryCategory)
                .NotEmpty()
                .Must(BeAValidCategory)
                    .WithMessage("PrimaryCategory must be a valid WasteCategory name.");
        });
    }

    private static bool BeAValidCategory(string value)
        => Enum.TryParse<Domain.Enums.WasteCategory>(value, ignoreCase: true, out _);
}
