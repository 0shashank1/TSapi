using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TS.Api.Extensions;
using TS.Application.DTOs.ShareLinks;
using TS.Application.Interfaces;

namespace TS.Api.Controllers;

[ApiController]
[Route("api/v1/share-links")]
[Authorize]
[Produces("application/json")]
public sealed class ShareLinksController : ControllerBase
{
    private readonly IShareLinkService _shareLinkService;

    public ShareLinksController(IShareLinkService shareLinkService)
    {
        _shareLinkService = shareLinkService;
    }

    [HttpGet("{linkId}")]
    [ProducesResponseType<ShareLinkResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid linkId,
        CancellationToken cancellationToken)
    {
        var response = await _shareLinkService.GetAsync(
            User.ToAccessContext(), linkId, cancellationToken);

        return Ok(response);
    }

    [HttpPatch("{linkId}")]
    [ProducesResponseType<ShareLinkResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        Guid linkId,
        UpdateShareLinkRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _shareLinkService.UpdateAsync(
            User.ToAccessContext(), linkId, request, cancellationToken);

        return Ok(response);
    }

    [HttpDelete("{linkId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Revoke(
        Guid linkId,
        CancellationToken cancellationToken)
    {
        await _shareLinkService.RevokeAsync(
            User.ToAccessContext(), linkId, cancellationToken);

        return NoContent();
    }
}
