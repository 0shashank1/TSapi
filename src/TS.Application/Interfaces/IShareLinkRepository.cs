using TS.Application.Common;
using TS.Domain.Entities;

namespace TS.Application.Interfaces;

public interface IShareLinkRepository : IRepository<ShareLink>
{
    /// <summary>Tracked fetch including the owning snippet.</summary>
    Task<ShareLink?> FindWithSnippetAsync(
        Guid shareLinkId,
        CancellationToken cancellationToken = default);

    /// <summary>Tracked fetch by stored token hash, including the snippet.</summary>
    Task<ShareLink?> FindByTokenAsync(
        short keyVersion,
        byte[] tokenHash,
        CancellationToken cancellationToken = default);

    Task<List<ShareLink>> ListBySnippetAsync(
        Guid snippetId,
        DateKeyset? cursor,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes links that expired or were revoked at or before
    /// <paramref name="expiredBeforeUtc"/>, at most
    /// <paramref name="limit"/> per call. Access logs keep their history
    /// (the foreign key is set to null by the database).
    /// </summary>
    Task<int> PurgeExpiredAsync(
        DateTime expiredBeforeUtc,
        int limit,
        CancellationToken cancellationToken = default);
}
