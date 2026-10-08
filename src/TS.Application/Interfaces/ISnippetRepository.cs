using TS.Application.Common;
using TS.Application.DTOs.Snippets;
using TS.Domain.Entities;

namespace TS.Application.Interfaces;

public interface ISnippetRepository : IRepository<TextSnippet>
{
    Task<List<TextSnippet>> ListOwnedAsync(
        Guid ownerId,
        DateTime utcNow,
        SnippetStatus status,
        string? search,
        SnippetSortBy sortBy,
        bool descending,
        Keyset? cursor,
        int limit,
        CancellationToken cancellationToken = default);

    Task<int> CountByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default);
}
