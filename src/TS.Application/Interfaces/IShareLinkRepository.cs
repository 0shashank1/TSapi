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
}
