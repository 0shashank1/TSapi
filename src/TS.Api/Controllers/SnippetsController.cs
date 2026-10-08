using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TS.Api.Extensions;
using TS.Application.Common;
using TS.Application.DTOs.ShareLinks;
using TS.Application.DTOs.Snippets;
using TS.Application.Interfaces;

namespace TS.Api.Controllers;

[ApiController]
[Route("api/v1/snippets")]
[Authorize]
[Produces("application/json")]
public sealed class SnippetsController : ControllerBase
{
    private readonly ISnippetService _snippetService;
    private readonly IShareLinkService _shareLinkService;

    public SnippetsController(
        ISnippetService snippetService,
        IShareLinkService shareLinkService)
    {
        _snippetService = snippetService;
        _shareLinkService = shareLinkService;
    }

    [HttpPost]
    [ProducesResponseType<SnippetResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        CreateSnippetRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _snippetService.CreateAsync(
            User.ToAccessContext(), request, cancellationToken);

        Response.Headers.Location =
            $"/api/v1/snippets/{response.Id}";

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet]
    [ProducesResponseType<PagedResponse<SnippetListItem>>(
        StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] SnippetListQuery query,
        CancellationToken cancellationToken)
    {
        var response = await _snippetService.ListAsync(
            User.ToAccessContext(), query, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{snippetId}")]
    [ProducesResponseType<SnippetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid snippetId,
        CancellationToken cancellationToken)
    {
        var response = await _snippetService.GetAsync(
            User.ToAccessContext(), snippetId, cancellationToken);

        Response.Headers.ETag = $"\"{response.Version}\"";

        return Ok(response);
    }

    [HttpPatch("{snippetId}")]
    [ProducesResponseType<SnippetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid snippetId,
        UpdateSnippetRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _snippetService.UpdateAsync(
            User.ToAccessContext(),
            snippetId,
            request,
            ParseIfMatchVersion(),
            cancellationToken);

        Response.Headers.ETag = $"\"{response.Version}\"";

        return Ok(response);
    }

    [HttpDelete("{snippetId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        Guid snippetId,
        CancellationToken cancellationToken)
    {
        await _snippetService.DeleteAsync(
            User.ToAccessContext(), snippetId, cancellationToken);

        return NoContent();
    }

    [HttpPost("{snippetId}/links")]
    [ProducesResponseType<ShareLinkResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateLink(
        Guid snippetId,
        CreateShareLinkRequest request,
        CancellationToken cancellationToken)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var response = await _shareLinkService.CreateAsync(
            User.ToAccessContext(),
            snippetId,
            request,
            baseUrl,
            cancellationToken);

        return Created(
            $"/api/v1/share-links/{response.Id}",
            response);
    }

    [HttpGet("{snippetId}/links")]
    [ProducesResponseType<PagedResponse<ShareLinkListItem>>(
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLinks(
        Guid snippetId,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _shareLinkService.ListForSnippetAsync(
            User.ToAccessContext(),
            snippetId,
            pageSize,
            cursor,
            cancellationToken);

        return Ok(response);
    }

    private Guid? ParseIfMatchVersion()
    {
        var header = Request.Headers.IfMatch.ToString();

        if (string.IsNullOrWhiteSpace(header))
            return null;

        var value = header.Trim();

        if (value.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
            value = value[2..].Trim();

        value = value.Trim('"');

        if (!Guid.TryParse(value, out var version))
        {
            throw new ValidationException(
                "Invalid If-Match header.",
                new Dictionary<string, string[]>
                {
                    ["If-Match"] = [
                        "If-Match must contain the snippet version GUID from ETag."]
                });
        }

        return version;
    }
}
