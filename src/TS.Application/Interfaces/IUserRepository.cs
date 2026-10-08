using TS.Application.Common;
using TS.Domain.Entities;
using TS.Domain.Enums;

namespace TS.Application.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<bool> EmailExistsAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default);

    /// <summary>Tracked fetch: callers may mutate the returned user.</summary>
    Task<User?> FindByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default);

    Task<List<User>> SearchAsync(
        string? search,
        bool? isActive,
        UserRole? role,
        DateKeyset? cursor,
        int limit,
        CancellationToken cancellationToken = default);
}
