using Microsoft.EntityFrameworkCore;
using TS.Application.Common;
using TS.Application.Interfaces;
using TS.Domain.Entities;
using TS.Domain.Enums;

namespace TS.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(TSDbContext dbContext)
    : EfRepository<User>(dbContext), IUserRepository
{
    public Task<bool> EmailExistsAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
        => DbSet.AsNoTracking()
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

    public async Task<User?> FindByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(
            u => u.NormalizedEmail == normalizedEmail,
            cancellationToken);

    public async Task<List<User>> SearchAsync(
        string? search,
        bool? isActive,
        UserRole? role,
        DateKeyset? cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        IQueryable<User> query = DbSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                u.Email.Contains(term) ||
                (u.DisplayName != null && u.DisplayName.Contains(term)));
        }

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        if (cursor is DateKeyset keyset)
        {
            query = query.Where(u =>
                u.CreatedAtUtc < keyset.Value ||
                (u.CreatedAtUtc == keyset.Value &&
                 u.Id.CompareTo(keyset.Id) < 0));
        }

        return await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .ThenByDescending(u => u.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
