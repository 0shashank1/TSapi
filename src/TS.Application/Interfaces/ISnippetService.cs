using TS.Application.Common;
using TS.Application.DTOs.Snippets;

namespace TS.Application.Interfaces;

public interface ISnippetService
{
    Task<SnippetResponse> CreateAsync(
        AccessContext context,
        CreateSnippetRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<SnippetListItem>> ListAsync(
        AccessContext context,
        SnippetListQuery query,
        CancellationToken cancellationToken = default);

    Task<SnippetResponse> GetAsync(
        AccessContext context,
        Guid snippetId,
        CancellationToken cancellationToken = default);

    Task<SnippetResponse> UpdateAsync(
        AccessContext context,
        Guid snippetId,
        UpdateSnippetRequest request,
        Guid? ifMatchVersion,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        AccessContext context,
        Guid snippetId,
        CancellationToken cancellationToken = default);
}
