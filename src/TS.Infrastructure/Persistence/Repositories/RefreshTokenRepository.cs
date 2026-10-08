using Microsoft.EntityFrameworkCore;
using TS.Application.Common;
using TS.Application.DTOs.Admin;
using TS.Application.Interfaces;
using TS.Domain.Entities;

namespace TS.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository(TSDbContext dbContext)
    : EfRepository<RefreshToken>(dbContext), IRefreshTokenRepository
{
    public async Task<RefreshToken?> FindByTokenHashAsync(
        byte[] tokenHash,
        CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(
            t => t.TokenHash == tokenHash,
            cancellationToken);

    public async Task<RefreshToken?> FindByTokenHashWithUserAsync(
        byte[] tokenHash,
        CancellationToken cancellationToken = default)
        => await DbSet
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task<List<RefreshToken>> ListActiveByFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
        => await DbSet
            .Where(t => t.FamilyId == familyId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

    public async Task<List<RefreshToken>> ListActiveByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => await DbSet
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

    public Task<int> CountActiveAsync(
        Guid userId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
        => DbSet.AsNoTracking().CountAsync(
            t => t.UserId == userId &&
                 t.RevokedAtUtc == null &&
                 t.ExpiresAtUtc > utcNow,
            cancellationToken);

    public async Task<List<RefreshToken>> SearchAsync(
        Guid? userId,
        RefreshTokenStatus status,
        DateKeyset? cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        IQueryable<RefreshToken> query = DbSet.AsNoTracking();

        if (userId.HasValue)
            query = query.Where(t => t.UserId == userId.Value);

        var now = DateTime.UtcNow;

        query = status switch
        {
            RefreshTokenStatus.All => query,
            RefreshTokenStatus.Active => query.Where(
                t => t.RevokedAtUtc == null && t.ExpiresAtUtc > now),
            RefreshTokenStatus.Revoked => query.Where(
                t => t.RevokedAtUtc != null),
            _ => query
        };

        if (cursor is DateKeyset keyset)
        {
            query = query.Where(t =>
                t.CreatedAtUtc < keyset.Value ||
                (t.CreatedAtUtc == keyset.Value &&
                 t.Id.CompareTo(keyset.Id) < 0));
        }

        return await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .ThenByDescending(t => t.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
