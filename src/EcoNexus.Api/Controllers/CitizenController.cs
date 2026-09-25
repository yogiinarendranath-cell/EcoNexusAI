using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EcoNexus.Application.Features.Citizen.FileReport;
using EcoNexus.Application.Features.Citizen.ClassifyWaste;
using EcoNexus.Application.Features.Citizen.ListClassifications;
using EcoNexus.Application.Features.Citizen.GetProfile;
using EcoNexus.Application.Features.Citizen.ListMyReports;
using EcoNexus.Application.Features.Citizen.ListPointTransactions;
using EcoNexus.Application.Features.Citizen.ListRewards;
using EcoNexus.Application.Features.Citizen.RecordStationVisit;
using EcoNexus.Application.Features.Citizen.RedeemReward;
using EcoNexus.Contracts.Citizen;
using EcoNexus.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoNexus.Api.Controllers;

[ApiController]
[Route("api/v1/citizen")]
[Authorize]
public sealed class CitizenController : ControllerBase
{
    private readonly ISender _sender;

    public CitizenController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Returns the current citizen's profile. Creates the profile on
    /// first call if the user has not been provisioned yet.
    /// </summary>
    [HttpGet("profile")]
    [Authorize(Roles = EcoNexusRoles.Citizen)]
    [ProducesResponseType(typeof(CitizenProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var displayName = User.FindFirstValue(ClaimTypes.Name) ?? "Citizen";

        var profile = await _sender.Send(
            new GetCitizenProfileQuery(userId, displayName),
            cancellationToken);

        return Ok(profile);
    }

    /// <summary>Paginated green-points history for the current citizen.</summary>
    [HttpGet("points/history")]
    [Authorize(Roles = EcoNexusRoles.Citizen)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListPointHistory(
        [FromQuery] PointHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var result = await _sender.Send(
            new ListPointTransactionsQuery(userId, query),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>Records a station visit and awards green points.</summary>
    [HttpPost("visits")]
    [Authorize(Roles = EcoNexusRoles.Citizen)]
    [ProducesResponseType(typeof(RecordStationVisitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RecordVisit(
        [FromBody] RecordStationVisitRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var response = await _sender.Send(
            new RecordStationVisitCommand(userId, request),
            cancellationToken);

        return Ok(response);
    }

    /// <summary>Lists rewards available for redemption.</summary>
    [HttpGet("rewards")]
    [ProducesResponseType(typeof(IReadOnlyList<RewardResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRewards(CancellationToken cancellationToken)
    {
        var rewards = await _sender.Send(new ListRewardsQuery(), cancellationToken);
        return Ok(rewards);
    }

    /// <summary>Redeems a reward using the citizen's green points.</summary>
    [HttpPost("rewards/{id:guid}/redeem")]
    [Authorize(Roles = EcoNexusRoles.Citizen)]
    [ProducesResponseType(typeof(RedeemRewardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RedeemReward(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var response = await _sender.Send(
            new RedeemRewardCommand(userId, id),
            cancellationToken);

        return Ok(response);
    }

    /// <summary>Files a citizen report against a waste station.</summary>
    [HttpPost("reports")]
    [Authorize(Roles = EcoNexusRoles.Citizen)]
    [ProducesResponseType(typeof(FileReportResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FileReport(
        [FromBody] FileReportRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var response = await _sender.Send(
            new FileReportCommand(userId, request),
            cancellationToken);

        return CreatedAtAction(nameof(ListMyReports), new { id = response.Id }, response);
    }

    /// <summary>Lists all reports filed by the current citizen.</summary>
    [HttpGet("reports/mine")]
    [Authorize(Roles = EcoNexusRoles.Citizen)]
    [ProducesResponseType(typeof(IReadOnlyList<CitizenReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListMyReports(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var reports = await _sender.Send(
            new ListMyReportsQuery(userId),
            cancellationToken);

        return Ok(reports);
    }

    /// <summary>
    /// Classifies a waste image via the configured AI provider and
    /// records the result on the citizen's profile.
    /// </summary>
    [HttpPost("classify")]
    [Authorize(Roles = EcoNexusRoles.Citizen)]
    [ProducesResponseType(typeof(ClassifyWasteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ClassifyWaste(
        [FromBody] ClassifyWasteRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var response = await _sender.Send(
            new ClassifyWasteCommand(userId, request.ImageUrl),
            cancellationToken);

        return Ok(response);
    }

    /// <summary>Lists the current citizen's AI classification history, newest first.</summary>
    [HttpGet("classifications")]
    [Authorize(Roles = EcoNexusRoles.Citizen)]
    [ProducesResponseType(typeof(IReadOnlyList<ClassificationListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListClassifications(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var classifications = await _sender.Send(
            new ListClassificationsQuery(userId),
            cancellationToken);

        return Ok(classifications);
    }

    /// <summary>
    /// Resolves the current user's id from the JWT. Mirrors the same
    /// lookup used by AuthController.Me.
    /// </summary>
    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (claim is null || !Guid.TryParse(claim, out var userId))
        {
            throw new UnauthorizedAccessException(
                "Access token does not contain a valid user identifier.");
        }

        return userId;
    }
}
