using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TS.Api.Extensions;
using TS.Application.DTOs.Auth;
using TS.Application.Interfaces;

namespace TS.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    private string? ClientIp =>
        HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? UserAgent =>
        Request.Headers.UserAgent.ToString();

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.RegisterAsync(
            request, ClientIp, UserAgent, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(
            request, ClientIp, UserAgent, cancellationToken);

        return Ok(response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Refresh(
        RefreshRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.RefreshAsync(
            request.RefreshToken, ClientIp, UserAgent, cancellationToken);

        return Ok(response);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(
        LogoutRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(
            request.RefreshToken, ClientIp, cancellationToken);

        return NoContent();
    }

    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        await _authService.LogoutAllAsync(
            User.GetUserId(), ClientIp, cancellationToken);

        return NoContent();
    }
}
