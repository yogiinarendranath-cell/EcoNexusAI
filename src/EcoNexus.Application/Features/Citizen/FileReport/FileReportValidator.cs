using EcoNexus.Domain.Enums;
using FluentValidation;

namespace EcoNexus.Application.Features.Citizen.FileReport;

internal sealed class FileReportValidator : AbstractValidator<FileReportCommand>
{
    public FileReportValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Request).NotNull();

        When(x => x.Request is not null, () =>
        {
            RuleFor(x => x.Request.StationId)
                .NotEmpty();

            RuleFor(x => x.Request.ReportType)
                .NotEmpty()
                .Must(BeAValidReportType)
                    .WithMessage("ReportType must be a valid CitizenReportType name.");

            RuleFor(x => x.Request.Description)
                .NotEmpty()
                .MaximumLength(2000);

            RuleFor(x => x.Request.PhotoUrl)
                .MaximumLength(2048)
                .When(x => !string.IsNullOrWhiteSpace(x.Request.PhotoUrl));
        });
    }

    private static bool BeAValidReportType(string value)
        => Enum.TryParse<CitizenReportType>(value, ignoreCase: true, out _);
}
