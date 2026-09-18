using EcoNexus.Application.Features.Stations.CreateStation;
using EcoNexus.Application.Features.Stations.GetStationById;
using EcoNexus.Contracts.Stations;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EcoNexus.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class StationsController : ControllerBase
{
    private readonly ISender _sender;

    public StationsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Create a new waste station.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateStationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateStationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new CreateStationCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>Get a waste station by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StationDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var station = await _sender.Send(new GetStationByIdQuery(id), cancellationToken);

        if (station is null)
        {
            return NotFound();
        }

        return Ok(station);
    }
}
