using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EcoNexus.Application.Abstractions.Identity;
using EcoNexus.Contracts.Auth;
using EcoNexus.Infrastructure.Identity;
using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EcoNexus.Api.Controllers;

/// <summary>
/// Authentication endpoints: register, login, refresh, logout, and me.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshTokenValidator;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshTokenRequest> refreshTokenValidator,
        TimeProvider timeProvider,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshTokenValidator = refreshTokenValidator;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    // ============================================================
    // POST /api/v1/auth/register
    // ============================================================
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationProblem(validation));
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName.Trim(),
            CreatedAt = _timeProvider.GetUtcNow()
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var duplicate = result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.DuplicateEmail));
            if (duplicate)
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Email already registered",
                    Detail = "An account with this email already exists."
                });
            }

            return BadRequest(BuildValidationProblem(result.Errors));
        }

        await _userManager.AddToRoleAsync(user, EcoNexusRoles.Citizen);

        _logger.LogInformation("Registered user {UserId} ({Email})", user.Id, user.Email);

        var response = new RegisterResponse(
            user.Id,
            user.Email!,
            user.DisplayName,
            new[] { EcoNexusRoles.Citizen });

        return StatusCode(StatusCodes.Status201Created, response);
    }

    // ============================================================
    // POST /api/v1/auth/login
    // ============================================================
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationProblem(validation));
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            _logger.LogWarning("Login failed: unknown email {Email}", request.Email);
            return Unauthorized();
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            _logger.LogWarning("Login failed for user {UserId}: {Reason}", user.Id, signInResult.ToString());
            return Unauthorized();
        }

        user.LastLoginAt = _timeProvider.GetUtcNow();
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var pair = await _tokenService.IssueTokenPairAsync(user.Id, user.Email!, roles, cancellationToken);

        _logger.LogInformation("User {UserId} logged in", user.Id);

        var response = new AuthResponse(
            pair.AccessToken,
            pair.AccessTokenExpiresAt,
            pair.RefreshToken,
            pair.RefreshTokenExpiresAt,
            new UserSummary(user.Id, user.Email!, user.DisplayName, roles.ToArray()));

        return Ok(response);
    }

    // ============================================================
    // POST /api/v1/auth/refresh
    // ============================================================
    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _refreshTokenValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationProblem(validation));
        }

        try
        {
            var pair = await _tokenService.RefreshAsync(request.RefreshToken, cancellationToken);

            // Re-query the user to include current user summary
            var handler = new JwtSecurityTokenHandler();
            var accessToken = handler.ReadJwtToken(pair.AccessToken);
            var userIdClaim = accessToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogError("Issued access token has no sub claim");
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                return Unauthorized();
            }

            var roles = await _userManager.GetRolesAsync(user);

            var response = new AuthResponse(
                pair.AccessToken,
                pair.AccessTokenExpiresAt,
                pair.RefreshToken,
                pair.RefreshTokenExpiresAt,
                new UserSummary(user.Id, user.Email!, user.DisplayName, roles.ToArray()));

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Refresh failed");
            return Unauthorized();
        }
    }

    // ============================================================
    // POST /api/v1/auth/logout
    // ============================================================
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _refreshTokenValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(BuildValidationProblem(validation));
        }

        await _tokenService.RevokeAsync(request.RefreshToken, cancellationToken);
        return NoContent();
    }

    // ============================================================
    // GET /api/v1/auth/me
    // ============================================================
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Unauthorized();
        }

        var roles = await _userManager.GetRolesAsync(user);

        var response = new CurrentUserResponse(
            user.Id,
            user.Email!,
            user.DisplayName,
            roles.ToArray());

        return Ok(response);
    }

    // ============================================================
    // Helpers
    // ============================================================

    private static ValidationProblemDetails BuildValidationProblem(
        FluentValidation.Results.ValidationResult validation)
    {
        var errors = validation.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred."
        };
    }

    private static ValidationProblemDetails BuildValidationProblem(
        IEnumerable<IdentityError> errors)
    {
        var dict = errors
            .GroupBy(e => e.Code)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.Description).ToArray());

        return new ValidationProblemDetails(dict)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred."
        };
    }
}
