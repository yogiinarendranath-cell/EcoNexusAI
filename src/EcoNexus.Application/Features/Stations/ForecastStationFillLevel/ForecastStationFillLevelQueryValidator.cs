using FluentValidation;

namespace EcoNexus.Application.Features.Stations.ForecastStationFillLevel;

/// <summary>
/// Input validation for <see cref="ForecastStationFillLevelQuery"/>.
/// </summary>
public sealed class ForecastStationFillLevelQueryValidator : AbstractValidator<ForecastStationFillLevelQuery>
{
    public ForecastStationFillLevelQueryValidator()
    {
        RuleFor(x => x.StationId)
            .NotEmpty()
            .WithMessage("StationId must not be empty.");

        RuleFor(x => x.WindowHours)
            .InclusiveBetween(1, 168)
            .WithMessage("WindowHours must be between 1 and 168 (7 days).");
    }
}

