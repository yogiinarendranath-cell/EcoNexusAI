using EcoNexus.Contracts.Citizen;
using MediatR;

namespace EcoNexus.Application.Features.Citizen.ListMyReports;

/// <summary>Query: list every report filed by the current citizen, newest first.</summary>
public sealed record ListMyReportsQuery(Guid UserId)
    : IRequest<IReadOnlyList<CitizenReportResponse>>;
