using FluentValidation;

namespace EcoNexus.Application.Features.Stations.ListStationReadings;

/// <summary>
/// Input validation for <see cref="ListStationReadingsQuery"/>.
/// </summary>
public sealed class ListStationReadingsQueryValidator : AbstractValidator<ListStationReadingsQuery>
{
    public ListStationReadingsQueryValidator()
    {
        RuleFor(x => x.StationId)
            .NotEmpty()
            .WithMessage("StationId must not be empty.");

        RuleFor(x => x.WindowHours)
            .InclusiveBetween(1, 168)
            .WithMessage("WindowHours must be between 1 and 168 (7 days).");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 500)
            .WithMessage("Limit must be between 1 and 500.");
    }
}

