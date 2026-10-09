using Microsoft.EntityFrameworkCore;
using TS.Application.Common;
using TS.Application.Interfaces;
using TS.Domain.Entities;

namespace TS.Infrastructure.Persistence.Repositories;

public sealed class ShareAccessLogRepository(TSDbContext dbContext)
    : EfRepository<ShareAccessLog>(dbContext), IShareAccessLogRepository
{
    public async Task<List<ShareAccessLog>> SearchAsync(
        bool? success,
        string? reason,
        DateTime? from,
        DateTime? to,
        DateKeyset? cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ShareAccessLog> query = DbSet.AsNoTracking();

        if (success.HasValue)
            query = query.Where(l => l.WasSuccessful == success.Value);

        if (!string.IsNullOrWhiteSpace(reason))
        {
            var value = reason.Trim();
            query = query.Where(l => l.FailureReason == value);
        }

        if (from.HasValue)
        {
            var start = from.Value.ToUniversalTime();
            query = query.Where(l => l.AccessedAtUtc >= start);
        }

        if (to.HasValue)
        {
            var end = to.Value.ToUniversalTime();
            query = query.Where(l => l.AccessedAtUtc < end);
        }

        if (cursor is DateKeyset keyset)
        {
            query = query.Where(l =>
                l.AccessedAtUtc < keyset.Value ||
                (l.AccessedAtUtc == keyset.Value &&
                 l.Id.CompareTo(keyset.Id) < 0));
        }

        return await query
            .OrderByDescending(l => l.AccessedAtUtc)
            .ThenByDescending(l => l.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public Task<int> PurgeOlderThanAsync(
        DateTime accessedBeforeUtc,
        int limit,
        CancellationToken cancellationToken = default)
        => DeleteBatchAsync(
            DbSet.Where(l => l.AccessedAtUtc <= accessedBeforeUtc),
            limit,
            cancellationToken);
}
