using EcoNexus.Contracts.Citizen;
using MediatR;

namespace EcoNexus.Application.Features.Citizen.ClassifyWaste;

/// <summary>
/// Command: classify a waste image via the AI service and record the
/// result on the citizen's profile.
/// </summary>
public sealed record ClassifyWasteCommand(
    Guid UserId,
    string ImageUrl) : IRequest<ClassifyWasteResponse>;
