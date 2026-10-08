using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TS.Application.DTOs.Public;
using TS.Application.Interfaces;

namespace TS.Api.Controllers;

[ApiController]
[Route("s")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class PublicShareController : ControllerBase
{
    private const string BearerPrefix = "Bearer ";

    private readonly IPublicShareService _publicShareService;

    public PublicShareController(IPublicShareService publicShareService)
    {
        _publicShareService = publicShareService;
    }

    private string? ClientIp =>
        HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? UserAgent =>
        Request.Headers.UserAgent.ToString();

    [HttpGet("{code}")]
    [EnableRateLimiting("share")]
    [ProducesResponseType<PublicShareResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Get(
        string code,
        CancellationToken cancellationToken)
    {
        var response = await _publicShareService.GetAsync(
            code,
            ExtractShareAccessToken(),
            ClientIp,
            UserAgent,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("{code}/unlock")]
    [EnableRateLimiting("unlock")]
    [ProducesResponseType<UnlockResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Unlock(
        string code,
        UnlockRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _publicShareService.UnlockAsync(
            code,
            request.Password,
            ClientIp,
            UserAgent,
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// The share access token uses a different audience than user JWTs,
    /// so the bearer header is read manually instead of relying on the
    /// JWT authentication middleware.
    /// </summary>
    private string? ExtractShareAccessToken()
    {
        var header = Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(header) ||
            !header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = header[BearerPrefix.Length..].Trim();

        return string.IsNullOrEmpty(token) ? null : token;
    }
}
