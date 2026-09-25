using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Contracts.Citizen;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Enums;
using MediatR;

namespace EcoNexus.Application.Features.Citizen.FileReport;

internal sealed class FileReportHandler
    : IRequestHandler<FileReportCommand, FileReportResponse>
{
    private readonly ICitizenReportRepository _reportRepository;
    private readonly IWasteStationRepository _stationRepository;

    public FileReportHandler(
        ICitizenReportRepository reportRepository,
        IWasteStationRepository stationRepository)
    {
        _reportRepository = reportRepository;
        _stationRepository = stationRepository;
    }

    public async Task<FileReportResponse> Handle(
        FileReportCommand request,
        CancellationToken cancellationToken)
    {
        var station = await _stationRepository.GetByIdAsync(
            request.Request.StationId,
            cancellationToken);

        if (station is null)
        {
            throw new NotFoundException(
                $"Station '{request.Request.StationId}' was not found.");
        }

        var reportType = Enum.Parse<CitizenReportType>(
            request.Request.ReportType,
            ignoreCase: true);

        var filedAt = DateTimeOffset.UtcNow;

        var report = CitizenReport.File(
            request.UserId,
            request.Request.StationId,
            reportType,
            request.Request.Description,
            request.Request.PhotoUrl,
            filedAt);

        await _reportRepository.AddAsync(report, cancellationToken);

        return new FileReportResponse(
            report.Id,
            report.StationId,
            report.ReportType?.ToString() ?? CitizenReportType.Other.ToString(),
            report.Status.ToString(),
            report.FiledAt);
    }
}
