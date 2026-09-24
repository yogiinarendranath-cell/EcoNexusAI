using EcoNexus.Application.Features.CollectionJobs.GetJobById;
using EcoNexus.Application.Features.CollectionJobs.ListJobs;
using EcoNexus.Application.Features.CollectionJobs.PreviewRoute;
using EcoNexus.Application.Features.CollectionJobs.ScheduleJob;
using EcoNexus.Contracts.CollectionJobs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EcoNexus.Api.Controllers;

[ApiController]
[Route("api/v1/collection-jobs")]
public sealed class CollectionJobsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CollectionJobsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CollectionJobResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var jobs = await _mediator.Send(new ListJobsQuery(), cancellationToken);
        return Ok(jobs);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CollectionJobResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var job = await _mediator.Send(new GetJobByIdQuery(id), cancellationToken);
        return Ok(job);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CollectionJobResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Schedule(
        [FromBody] ScheduleJobRequest request,
        CancellationToken cancellationToken)
    {
        var job = await _mediator.Send(new ScheduleJobCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = job.Id }, job);
    }

    [HttpPost("preview-route")]
    [ProducesResponseType(typeof(PreviewRouteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviewRoute(
        [FromBody] PreviewRouteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new PreviewRouteQuery(request), cancellationToken);
        return Ok(result);
    }
}