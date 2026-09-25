using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Contracts.Citizen;
using EcoNexus.Domain.Enums;
using MediatR;

namespace EcoNexus.Application.Features.Citizen.ListMyReports;

internal sealed class ListMyReportsHandler
    : IRequestHandler<ListMyReportsQuery, IReadOnlyList<CitizenReportResponse>>
{
    private readonly ICitizenReportRepository _repository;

    public ListMyReportsHandler(ICitizenReportRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<CitizenReportResponse>> Handle(
        ListMyReportsQuery request,
        CancellationToken cancellationToken)
    {
        var reports = await _repository.ListByUserAsync(request.UserId, cancellationToken);

        return reports
            .Select(r => new CitizenReportResponse(
                r.Id,
                r.StationId,
                r.ReportType?.ToString() ?? CitizenReportType.Other.ToString(),
                r.Description,
                r.PhotoUrl,
                r.Status.ToString(),
                r.FiledAt,
                r.AcknowledgedAt,
                r.ResolvedAt,
                r.ResolutionNote))
            .ToList();
    }
}
