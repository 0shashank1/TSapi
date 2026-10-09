using Microsoft.EntityFrameworkCore;
using TS.Application.Common;
using TS.Application.DTOs.Snippets;
using TS.Application.Interfaces;
using TS.Domain.Entities;

namespace TS.Infrastructure.Persistence.Repositories;

public sealed class SnippetRepository(TSDbContext dbContext)
    : EfRepository<TextSnippet>(dbContext), ISnippetRepository
{
    public async Task<List<TextSnippet>> ListOwnedAsync(
        Guid ownerId,
        DateTime utcNow,
        SnippetStatus status,
        string? search,
        SnippetSortBy sortBy,
        bool descending,
        Keyset? cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TextSnippet> query = DbSet.AsNoTracking()
            .Where(s => s.OwnerUserId == ownerId);

        query = status switch
        {
            SnippetStatus.All => query,
            SnippetStatus.Active => query.Where(
                s => s.ExpiresAtUtc == null || s.ExpiresAtUtc > utcNow),
            SnippetStatus.Expired => query.Where(
                s => s.ExpiresAtUtc != null && s.ExpiresAtUtc <= utcNow),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(
                s => (s.Title != null && s.Title.Contains(term)) ||
                     s.Content.Contains(term));
        }

        query = ApplyCursor(query, sortBy, cursor, descending);
        query = ApplyOrdering(query, sortBy, descending);

        return await query.Take(limit).ToListAsync(cancellationToken);
    }

    public Task<int> CountByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
        => DbSet.AsNoTracking()
            .CountAsync(s => s.OwnerUserId == ownerId, cancellationToken);

    public Task<int> PurgeExpiredAsync(
        DateTime expiredBeforeUtc,
        int limit,
        CancellationToken cancellationToken = default)
        => DeleteBatchAsync(
            DbSet.Where(s =>
                s.ExpiresAtUtc != null && s.ExpiresAtUtc <= expiredBeforeUtc),
            limit,
            cancellationToken);

    private static IQueryable<TextSnippet> ApplyCursor(
        IQueryable<TextSnippet> source,
        SnippetSortBy sortBy,
        Keyset? cursor,
        bool descending)
    {
        if (cursor is null)
            return source;

        // Keyset filter: seek past the last row of the previous page in
        // whatever direction the result set is ordered.
        switch (sortBy, cursor)
        {
            case (SnippetSortBy.Title, StringKeyset keyset):
                source = descending
                    ? source.Where(s =>
                        (s.Title ?? "").CompareTo(keyset.Value) < 0 ||
                        ((s.Title ?? "") == keyset.Value &&
                         s.Id.CompareTo(keyset.Id) < 0))
                    : source.Where(s =>
                        (s.Title ?? "").CompareTo(keyset.Value) > 0 ||
                        ((s.Title ?? "") == keyset.Value &&
                         s.Id.CompareTo(keyset.Id) > 0));
                break;

            case (SnippetSortBy.ViewCount, NumberKeyset keyset):
                source = descending
                    ? source.Where(s =>
                        s.ViewCount < keyset.Value ||
                        (s.ViewCount == keyset.Value &&
                         s.Id.CompareTo(keyset.Id) < 0))
                    : source.Where(s =>
                        s.ViewCount > keyset.Value ||
                        (s.ViewCount == keyset.Value &&
                         s.Id.CompareTo(keyset.Id) > 0));
                break;

            case (SnippetSortBy.UpdatedAt, DateKeyset keyset):
                source = descending
                    ? source.Where(s =>
                        s.UpdatedAtUtc < keyset.Value ||
                        (s.UpdatedAtUtc == keyset.Value &&
                         s.Id.CompareTo(keyset.Id) < 0))
                    : source.Where(s =>
                        s.UpdatedAtUtc > keyset.Value ||
                        (s.UpdatedAtUtc == keyset.Value &&
                         s.Id.CompareTo(keyset.Id) > 0));
                break;

            case (SnippetSortBy.CreatedAt, DateKeyset keyset):
                source = descending
                    ? source.Where(s =>
                        s.CreatedAtUtc < keyset.Value ||
                        (s.CreatedAtUtc == keyset.Value &&
                         s.Id.CompareTo(keyset.Id) < 0))
                    : source.Where(s =>
                        s.CreatedAtUtc > keyset.Value ||
                        (s.CreatedAtUtc == keyset.Value &&
                         s.Id.CompareTo(keyset.Id) > 0));
                break;

            default:
                throw InvalidCursor();
        }

        return source;
    }

    private static IQueryable<TextSnippet> ApplyOrdering(
        IQueryable<TextSnippet> source,
        SnippetSortBy sortBy,
        bool descending)
    {
        return sortBy switch
        {
            SnippetSortBy.Title => descending
                ? source.OrderByDescending(s => s.Title ?? "")
                : source.OrderBy(s => s.Title ?? ""),
            SnippetSortBy.ViewCount => descending
                ? source.OrderByDescending(s => s.ViewCount)
                    .ThenByDescending(s => s.Id)
                : source.OrderBy(s => s.ViewCount)
                    .ThenBy(s => s.Id),
            SnippetSortBy.UpdatedAt => descending
                ? source.OrderByDescending(s => s.UpdatedAtUtc)
                    .ThenByDescending(s => s.Id)
                : source.OrderBy(s => s.UpdatedAtUtc)
                    .ThenBy(s => s.Id),
            _ => descending
                ? source.OrderByDescending(s => s.CreatedAtUtc)
                    .ThenByDescending(s => s.Id)
                : source.OrderBy(s => s.CreatedAtUtc)
                    .ThenBy(s => s.Id)
        };
    }

    private static ValidationException InvalidCursor()
        => new(
            "Invalid cursor.",
            new Dictionary<string, string[]>
            {
                ["cursor"] = ["The cursor is malformed or from a different query."]
            });
}
