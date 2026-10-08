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
}
