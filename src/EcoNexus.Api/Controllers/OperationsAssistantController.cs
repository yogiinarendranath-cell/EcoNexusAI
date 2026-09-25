using System.Security.Claims;
using EcoNexus.Application.Features.Operations.Assistant;
using EcoNexus.Contracts.Operations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoNexus.Api.Controllers;

/// <summary>
/// Operations assistant endpoints. Accessible only to operational roles —
/// the assistant exposes aggregated system state, which is not citizen-
/// facing data.
/// </summary>
[ApiController]
[Route("api/v1/operations/assistant")]
[Authorize(Roles = "SuperAdmin,CityAdmin,OperationsManager")]
public sealed class OperationsAssistantController : ControllerBase
{
    private readonly ISender _sender;

    public OperationsAssistantController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Ask the assistant a natural-language question about system state.
    /// Example: "Which stations are critical right now?"
    /// </summary>
    [HttpPost("ask")]
    [ProducesResponseType(typeof(AskAssistantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Ask(
        [FromBody] AskAssistantRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrThrow();

        var command = new AskAssistantCommand(userId, request.Question);
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    private Guid GetUserIdOrThrow()
    {
        // JWT middleware sets NameIdentifier from the "sub" claim because
        // MapInboundClaims is disabled in Program.cs. Fall back to "sub".
        var raw =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub");

        if (!Guid.TryParse(raw, out var userId))
        {
            throw new UnauthorizedAccessException(
                "Authenticated user has no valid subject claim.");
        }

        return userId;
    }
}
