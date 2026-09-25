using EcoNexus.Contracts.Citizen;
using MediatR;

namespace EcoNexus.Application.Features.Citizen.FileReport;

/// <summary>Command: file a citizen report against a waste station.</summary>
public sealed record FileReportCommand(Guid UserId, FileReportRequest Request)
    : IRequest<FileReportResponse>;
