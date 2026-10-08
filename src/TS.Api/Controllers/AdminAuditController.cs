using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TS.Application.Common;
using TS.Application.DTOs.Admin;
using TS.Application.Interfaces;

namespace TS.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = "AdminOnly")]
[Produces("application/json")]
public sealed class AdminAuditController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminAuditController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("access-logs")]
    [ProducesResponseType<PagedResponse<AccessLogListItem>>(
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccessLogs(
        [FromQuery] AccessLogQuery query,
        CancellationToken cancellationToken)
    {
        var response = await _adminService.ListAccessLogsAsync(
            query, cancellationToken);

        return Ok(response);
    }

    [HttpGet("refresh-tokens")]
    [ProducesResponseType<PagedResponse<RefreshTokenListItem>>(
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRefreshTokens(
        [FromQuery] RefreshTokenQuery query,
        CancellationToken cancellationToken)
    {
        var response = await _adminService.ListRefreshTokensAsync(
            query, cancellationToken);

        return Ok(response);
    }
}
