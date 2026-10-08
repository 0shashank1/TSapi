using TS.Application.Common;
using TS.Application.DTOs.Admin;
using TS.Domain.Entities;

namespace TS.Application.Interfaces;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> FindByTokenHashAsync(
        byte[] tokenHash,
        CancellationToken cancellationToken = default);

    /// <summary>Tracked fetch including the owning user (token rotation).</summary>
    Task<RefreshToken?> FindByTokenHashWithUserAsync(
        byte[] tokenHash,
        CancellationToken cancellationToken = default);

    Task<List<RefreshToken>> ListActiveByFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken = default);

    Task<List<RefreshToken>> ListActiveByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<int> CountActiveAsync(
        Guid userId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<List<RefreshToken>> SearchAsync(
        Guid? userId,
        RefreshTokenStatus status,
        DateKeyset? cursor,
        int limit,
        CancellationToken cancellationToken = default);
}
