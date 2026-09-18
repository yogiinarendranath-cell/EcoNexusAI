using FluentValidation;

namespace EcoNexus.Application.Features.Stations.RecordStationReading;

internal sealed class RecordStationReadingValidator
    : AbstractValidator<RecordStationReadingCommand>
{
    public RecordStationReadingValidator()
    {
        RuleFor(x => x.StationId).NotEmpty();
        RuleFor(x => x.Request).NotNull();

        When(x => x.Request is not null, () =>
        {
            RuleFor(x => x.Request.FillLevelPercent)
                .InclusiveBetween(0, 100);

            RuleFor(x => x.Request.TemperatureCelsius)
                .InclusiveBetween(-50, 100);

            RuleFor(x => x.Request.BatteryPercent)
                .InclusiveBetween(0, 100);

            RuleFor(x => x.Request.RecordedAt)
                .NotEqual(default(DateTimeOffset));
        });
    }
}
