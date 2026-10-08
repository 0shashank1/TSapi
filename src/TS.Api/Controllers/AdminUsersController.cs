using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TS.Application.Common;
using TS.Application.DTOs.Admin;
using TS.Application.Interfaces;

namespace TS.Api.Controllers;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Policy = "AdminOnly")]
[Produces("application/json")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminUsersController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    [ProducesResponseType<PagedResponse<AdminUserListItem>>(
        StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] AdminUserListQuery query,
        CancellationToken cancellationToken)
    {
        var response = await _adminService.ListUsersAsync(
            query, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{userId}")]
    [ProducesResponseType<AdminUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var response = await _adminService.GetUserAsync(
            userId, cancellationToken);

        return Ok(response);
    }

    [HttpPatch("{userId}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetStatus(
        Guid userId,
        AdminUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        await _adminService.SetStatusAsync(
            userId,
            request,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);

        return NoContent();
    }

    [HttpPatch("{userId}/role")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetRole(
        Guid userId,
        AdminUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        await _adminService.SetRoleAsync(
            userId, request, cancellationToken);

        return NoContent();
    }
}
