using TS.Application.Common;
using TS.Application.DTOs.ShareLinks;

namespace TS.Application.Interfaces;

public interface IShareLinkService
{
    Task<ShareLinkResponse> CreateAsync(
        AccessContext context,
        Guid snippetId,
        CreateShareLinkRequest request,
        string baseUrl,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<ShareLinkListItem>> ListForSnippetAsync(
        AccessContext context,
        Guid snippetId,
        int pageSize,
        string? cursor,
        CancellationToken cancellationToken = default);

    Task<ShareLinkResponse> GetAsync(
        AccessContext context,
        Guid linkId,
        CancellationToken cancellationToken = default);

    Task<ShareLinkResponse> UpdateAsync(
        AccessContext context,
        Guid linkId,
        UpdateShareLinkRequest request,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(
        AccessContext context,
        Guid linkId,
        CancellationToken cancellationToken = default);
}
