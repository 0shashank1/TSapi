using TS.Application.Common;
using TS.Domain.Entities;

namespace TS.Application.Interfaces;

public interface IShareAccessLogRepository : IRepository<ShareAccessLog>
{
    Task<List<ShareAccessLog>> SearchAsync(
        bool? success,
        string? reason,
        DateTime? from,
        DateTime? to,
        DateKeyset? cursor,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes audit rows accessed at or before
    /// <paramref name="accessedBeforeUtc"/>, at most
    /// <paramref name="limit"/> per call.
    /// </summary>
    Task<int> PurgeOlderThanAsync(
        DateTime accessedBeforeUtc,
        int limit,
        CancellationToken cancellationToken = default);
}
