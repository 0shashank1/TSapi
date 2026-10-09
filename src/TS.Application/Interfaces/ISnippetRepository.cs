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

    /// <summary>
    /// Removes snippets whose <c>ExpiresAtUtc</c> is at or before
    /// <paramref name="expiredBeforeUtc"/>, at most <paramref name="limit"/>
    /// per call. Share links cascade at the database.
    /// </summary>
    Task<int> PurgeExpiredAsync(
        DateTime expiredBeforeUtc,
        int limit,
        CancellationToken cancellationToken = default);
}
