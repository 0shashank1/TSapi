using Microsoft.EntityFrameworkCore;
using TS.Application.Common;
using TS.Application.Interfaces;
using TS.Domain.Entities;

namespace TS.Infrastructure.Persistence.Repositories;

public sealed class ShareLinkRepository(TSDbContext dbContext)
    : EfRepository<ShareLink>(dbContext), IShareLinkRepository
{
    public async Task<ShareLink?> FindWithSnippetAsync(
        Guid shareLinkId,
        CancellationToken cancellationToken = default)
        => await DbSet
            .Include(l => l.TextSnippet)
            .FirstOrDefaultAsync(l => l.Id == shareLinkId, cancellationToken);

    public async Task<ShareLink?> FindByTokenAsync(
        short keyVersion,
        byte[] tokenHash,
        CancellationToken cancellationToken = default)
        => await DbSet
            .Include(l => l.TextSnippet)
            .FirstOrDefaultAsync(
                l => l.TokenKeyVersion == keyVersion &&
                     l.TokenHash == tokenHash,
                cancellationToken);

    public async Task<List<ShareLink>> ListBySnippetAsync(
        Guid snippetId,
        DateKeyset? cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ShareLink> query = DbSet.AsNoTracking()
            .Where(l => l.TextSnippetId == snippetId);

        if (cursor is DateKeyset keyset)
        {
            query = query.Where(l =>
                l.CreatedAtUtc < keyset.Value ||
                (l.CreatedAtUtc == keyset.Value &&
                 l.Id.CompareTo(keyset.Id) < 0));
        }

        return await query
            .OrderByDescending(l => l.CreatedAtUtc)
            .ThenByDescending(l => l.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
