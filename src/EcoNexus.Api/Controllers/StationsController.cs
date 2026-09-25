using EcoNexus.Application.Features.Stations.CreateStation;
using EcoNexus.Application.Features.Stations.DeleteStation;
using EcoNexus.Application.Features.Stations.UpdateStation;
using EcoNexus.Application.Features.Stations.GetStationById;
using EcoNexus.Application.Features.Stations.ListStations;
using EcoNexus.Application.Features.Stations.RecordStationReading;
using EcoNexus.Contracts.Common;
using EcoNexus.Contracts.Stations;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
    [EnableRateLimiting("writes")]
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

    /// <summary>List stations with pagination and filters.</summary>
    [OutputCache(PolicyName = "StationsList")]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<StationListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] StationListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListStationsQuery(query), cancellationToken);
        return Ok(result);
    }

    /// <summary>Record a sensor reading for a station.</summary>
    [HttpPost("{id:guid}/readings")]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(typeof(RecordReadingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordReading(
        Guid id,
        [FromBody] RecordReadingRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new RecordStationReadingCommand(id, request),
            cancellationToken);

        return Ok(response);
    }

    /// <summary>Update the mutable metadata of an existing station.</summary>
    [HttpPut("{id:guid}")]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateStationRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdateStationCommand(id, request), cancellationToken);
        return NoContent();
    }

    /// <summary>Delete a station. Idempotent.</summary>
    [HttpDelete("{id:guid}")]
    [EnableRateLimiting("writes")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteStationCommand(id), cancellationToken);
        return NoContent();
    }
}
