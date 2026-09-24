using EcoNexus.Application.Features.CollectionVehicles.CreateVehicle;
using EcoNexus.Application.Features.CollectionVehicles.ListVehicles;
using EcoNexus.Contracts.CollectionVehicles;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EcoNexus.Api.Controllers;

[ApiController]
[Route("api/v1/collection-vehicles")]
public sealed class CollectionVehiclesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CollectionVehiclesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CollectionVehicleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var vehicles = await _mediator.Send(new ListVehiclesQuery(), cancellationToken);
        return Ok(vehicles);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CollectionVehicleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCollectionVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var vehicle = await _mediator.Send(new CreateVehicleCommand(request), cancellationToken);
        return CreatedAtAction(nameof(List), new { id = vehicle.Id }, vehicle);
    }
}
