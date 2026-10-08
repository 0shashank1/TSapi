using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TS.Api.Extensions;
using TS.Application.DTOs.Users;
using TS.Application.Interfaces;

namespace TS.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
[Produces("application/json")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var response = await _userService.GetMeAsync(
            User.GetUserId(), cancellationToken);

        return Ok(response);
    }

    [HttpPatch("me")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMe(
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _userService.UpdateProfileAsync(
            User.GetUserId(), request, cancellationToken);

        return Ok(response);
    }

    [HttpPatch("me/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _userService.ChangePasswordAsync(
            User.GetUserId(), request, cancellationToken);

        return NoContent();
    }

    [HttpDelete("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMe(CancellationToken cancellationToken)
    {
        await _userService.DeactivateAsync(
            User.GetUserId(), cancellationToken);

        return NoContent();
    }
}
