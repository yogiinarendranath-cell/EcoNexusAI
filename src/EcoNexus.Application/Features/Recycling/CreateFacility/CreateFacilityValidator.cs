using FluentValidation;

namespace EcoNexus.Application.Features.Recycling.CreateFacility;

internal sealed class CreateFacilityValidator : AbstractValidator<CreateFacilityCommand>
{
    public CreateFacilityValidator()
    {
        RuleFor(x => x.Request).NotNull();

        When(x => x.Request is not null, () =>
        {
            RuleFor(x => x.Request.Name)
                .NotEmpty()
                .MaximumLength(120);

            RuleFor(x => x.Request.Latitude)
                .InclusiveBetween(-90, 90);

            RuleFor(x => x.Request.Longitude)
                .InclusiveBetween(-180, 180);

            RuleFor(x => x.Request.DailyCapacityKilograms)
                .GreaterThan(0);
        });
    }
}
