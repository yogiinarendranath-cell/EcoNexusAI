using EcoNexus.Application.Features.Recycling.AdvanceIntake;
using EcoNexus.Application.Features.Recycling.CreateFacility;
using EcoNexus.Application.Features.Recycling.GetFacilityById;
using EcoNexus.Application.Features.Recycling.ListFacilities;
using EcoNexus.Application.Features.Recycling.RecordIntake;
using EcoNexus.Contracts.Recycling;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EcoNexus.Api.Controllers;

[ApiController]
[Route("api/v1/recycling-facilities")]
public sealed class RecyclingFacilitiesController : ControllerBase
{
    private readonly ISender _sender;

    public RecyclingFacilitiesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Create a new recycling facility.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateFacilityResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateFacilityRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new CreateFacilityCommand(request),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>List every recycling facility with summary metrics.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FacilityListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListFacilitiesQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Get a recycling facility by id, including its intake history and metrics.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FacilityDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var facility = await _sender.Send(
            new GetFacilityByIdQuery(id),
            cancellationToken);

        return Ok(facility);
    }

    /// <summary>Record a new intake batch at a facility.</summary>
    [HttpPost("{id:guid}/intakes")]
    [ProducesResponseType(typeof(RecordIntakeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordIntake(
        Guid id,
        [FromBody] RecordIntakeRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new RecordIntakeCommand(id, request),
            cancellationToken);

        return Ok(response);
    }

    /// <summary>Advance an intake batch to a new lifecycle stage.</summary>
    [HttpPost("{id:guid}/intakes/{intakeId:guid}/advance")]
    [ProducesResponseType(typeof(AdvanceIntakeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AdvanceIntake(
        Guid id,
        Guid intakeId,
        [FromBody] AdvanceIntakeRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new AdvanceIntakeCommand(id, intakeId, request),
            cancellationToken);

        return Ok(response);
    }
}
