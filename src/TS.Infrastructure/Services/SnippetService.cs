using Microsoft.EntityFrameworkCore;
using TS.Application.Common;
using TS.Application.DTOs.Snippets;
using TS.Application.Interfaces;
using TS.Domain.Entities;
using TS.Infrastructure.Persistence;

namespace TS.Infrastructure.Services;

public sealed class SnippetService : ISnippetService
{
    private readonly TSDbContext _db;

    public SnippetService(TSDbContext db)
    {
        _db = db;
    }

    public async Task<SnippetResponse> CreateAsync(
        AccessContext context,
        CreateSnippetRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        ValidateExpiry(request.ExpiresAtUtc, now);

        var snippet = new TextSnippet(
            context.UserId,
            request.Content,
            NormalizeTitle(request.Title),
            request.ExpiresAtUtc,
            request.MaxViews);

        _db.TextSnippets.Add(snippet);
        await _db.SaveChangesAsync(cancellationToken);

        return SnippetResponse.From(snippet);
    }

    public async Task<PagedResponse<SnippetListItem>> ListAsync(
        AccessContext context,
        SnippetListQuery query,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var sortBy = (query.SortBy ?? "createdAt").Trim().ToLowerInvariant();
        var status = (query.Status ?? "all").Trim().ToLowerInvariant();

        IQueryable<TextSnippet> source = _db.TextSnippets.AsNoTracking()
            .Where(s => s.OwnerUserId == context.UserId);

        source = status switch
        {
            "all" => source,
            "active" => source.Where(
                s => s.ExpiresAtUtc == null || s.ExpiresAtUtc > now),
            "expired" => source.Where(
                s => s.ExpiresAtUtc != null && s.ExpiresAtUtc <= now),
            _ => throw new ValidationException(
                "Invalid status filter.",
                new Dictionary<string, string[]>
                {
                    ["status"] = ["Must be one of: active, expired, all."]
                })
        };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            source = source.Where(
                s => (s.Title != null && s.Title.Contains(term)) ||
                     s.Content.Contains(term));
        }

        var descending = ResolveDescending(sortBy, query.SortDirection);

        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            source = ApplyCursor(source, sortBy, query.Cursor, descending);
        }

        var ordered = ApplyOrdering(source, sortBy, descending);

        var rows = await ordered
            .Take(query.PageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > query.PageSize;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);

        string? nextCursor = null;
        if (hasMore && rows.Count > 0)
        {
            var last = rows[^1];
            nextCursor = sortBy switch
            {
                "title" => Cursor.Encode(last.Title ?? string.Empty, last.Id),
                "viewcount" => Cursor.Encode(last.ViewCount, last.Id),
                "updatedat" => Cursor.Encode(last.UpdatedAtUtc.Ticks, last.Id),
                _ => Cursor.Encode(last.CreatedAtUtc.Ticks, last.Id)
            };
        }

        return new PagedResponse<SnippetListItem>(
            rows.Select(SnippetListItem.From).ToList(),
            nextCursor);
    }

    public async Task<SnippetResponse> GetAsync(
        AccessContext context,
        Guid snippetId,
        CancellationToken cancellationToken = default)
    {
        var snippet = await _db.TextSnippets
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == snippetId, cancellationToken);

        EnsureCanAccess(context, snippet);

        return SnippetResponse.From(snippet!);
    }

    public async Task<SnippetResponse> UpdateAsync(
        AccessContext context,
        Guid snippetId,
        UpdateSnippetRequest request,
        Guid? ifMatchVersion,
        CancellationToken cancellationToken = default)
    {
        var snippet = await _db.TextSnippets
            .FirstOrDefaultAsync(s => s.Id == snippetId, cancellationToken);

        EnsureCanAccess(context, snippet);

        var now = DateTime.UtcNow;

        if (ifMatchVersion.HasValue &&
            ifMatchVersion.Value != snippet!.Version)
        {
            throw new ConcurrencyConflictException(
                "The snippet was changed by another request.");
        }

        var title = request.Title is null
            ? snippet!.Title
            : NormalizeTitle(request.Title);

        var content = request.Content ?? snippet!.Content;

        var expiresAtUtc = request.ClearExpiresAtUtc
            ? null
            : request.ExpiresAtUtc ?? snippet!.ExpiresAtUtc;

        var maxViews = request.ClearMaxViews
            ? null
            : request.MaxViews ?? snippet!.MaxViews;

        ValidateExpiry(expiresAtUtc, now);

        snippet!.UpdateContent(content, title, expiresAtUtc, maxViews, now);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException(
                "The snippet was changed by another request.");
        }

        return SnippetResponse.From(snippet);
    }

    public async Task DeleteAsync(
        AccessContext context,
        Guid snippetId,
        CancellationToken cancellationToken = default)
    {
        var snippet = await _db.TextSnippets
            .FirstOrDefaultAsync(s => s.Id == snippetId, cancellationToken);

        EnsureCanAccess(context, snippet);

        _db.TextSnippets.Remove(snippet!);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureCanAccess(
        AccessContext context,
        TextSnippet? snippet)
    {
        if (snippet is null)
            throw new NotFoundException("Snippet not found.");

        if (snippet.OwnerUserId != context.UserId && !context.IsAdmin)
            throw new ForbiddenException(
                "You do not own this snippet.");
    }

    private static void ValidateExpiry(DateTime? expiresAtUtc, DateTime now)
    {
        if (expiresAtUtc.HasValue && expiresAtUtc.Value <= now)
        {
            throw new ValidationException(
                "Expiration must be in the future.",
                new Dictionary<string, string[]>
                {
                    ["expiresAtUtc"] = ["Expiration must be in the future."]
                });
        }
    }

    private static string? NormalizeTitle(string? title)
    {
        var trimmed = title?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static bool ResolveDescending(string sortBy, string? direction)
    {
        var explicitDirection = direction?.Trim().ToLowerInvariant();

        if (explicitDirection is not null and not ("asc" or "desc"))
        {
            throw new ValidationException(
                "Invalid sort direction.",
                new Dictionary<string, string[]>
                {
                    ["sortDirection"] = ["Must be one of: asc, desc."]
                });
        }

        if (explicitDirection is not null)
            return explicitDirection == "desc";

        return sortBy is not "title";
    }

    private static IQueryable<TextSnippet> ApplyOrdering(
        IQueryable<TextSnippet> source,
        string sortBy,
        bool descending)
    {
        return sortBy switch
        {
            "title" => descending
                ? source.OrderByDescending(s => s.Title ?? "")
                : source.OrderBy(s => s.Title ?? ""),
            "viewcount" => descending
                ? source.OrderByDescending(s => s.ViewCount)
                    .ThenByDescending(s => s.Id)
                : source.OrderBy(s => s.ViewCount)
                    .ThenBy(s => s.Id),
            "updatedat" => descending
                ? source.OrderByDescending(s => s.UpdatedAtUtc)
                    .ThenByDescending(s => s.Id)
                : source.OrderBy(s => s.UpdatedAtUtc)
                    .ThenBy(s => s.Id),
            "createdat" or _ => descending
                ? source.OrderByDescending(s => s.CreatedAtUtc)
                    .ThenByDescending(s => s.Id)
                : source.OrderBy(s => s.CreatedAtUtc)
                    .ThenBy(s => s.Id)
        };
    }

    private static IQueryable<TextSnippet> ApplyCursor(
        IQueryable<TextSnippet> source,
        string sortBy,
        string cursor,
        bool descending)
    {
        switch (sortBy)
        {
            case "title":
            {
                if (!Cursor.TryDecodeString(cursor, out var lastTitle, out var lastId))
                    throw InvalidCursor();

                return descending
                    ? source.Where(s =>
                        (s.Title ?? "").CompareTo(lastTitle) < 0 ||
                        ((s.Title ?? "") == lastTitle && s.Id.CompareTo(lastId) < 0))
                    : source.Where(s =>
                        (s.Title ?? "").CompareTo(lastTitle) > 0 ||
                        ((s.Title ?? "") == lastTitle && s.Id.CompareTo(lastId) > 0));
            }

            case "viewcount":
            {
                if (!Cursor.TryDecodeString(cursor, out var raw, out var lastId) ||
                    !long.TryParse(raw, out var lastValue))
                {
                    throw InvalidCursor();
                }

                return descending
                    ? source.Where(s =>
                        s.ViewCount < lastValue ||
                        (s.ViewCount == lastValue && s.Id.CompareTo(lastId) < 0))
                    : source.Where(s =>
                        s.ViewCount > lastValue ||
                        (s.ViewCount == lastValue && s.Id.CompareTo(lastId) > 0));
            }

            case "updatedat":
            {
                if (!TryDecodeTicks(cursor, out var lastValue, out var lastId))
                    throw InvalidCursor();

                return descending
                    ? source.Where(s =>
                        s.UpdatedAtUtc < lastValue ||
                        (s.UpdatedAtUtc == lastValue && s.Id.CompareTo(lastId) < 0))
                    : source.Where(s =>
                        s.UpdatedAtUtc > lastValue ||
                        (s.UpdatedAtUtc == lastValue && s.Id.CompareTo(lastId) > 0));
            }

            default:
            {
                if (!TryDecodeTicks(cursor, out var lastValue, out var lastId))
                    throw InvalidCursor();

                return descending
                    ? source.Where(s =>
                        s.CreatedAtUtc < lastValue ||
                        (s.CreatedAtUtc == lastValue && s.Id.CompareTo(lastId) < 0))
                    : source.Where(s =>
                        s.CreatedAtUtc > lastValue ||
                        (s.CreatedAtUtc == lastValue && s.Id.CompareTo(lastId) > 0));
            }
        }
    }

    private static bool TryDecodeTicks(
        string cursor,
        out DateTime value,
        out Guid id)
    {
        value = default;
        id = default;

        if (!Cursor.TryDecodeString(cursor, out var raw, out id) ||
            !long.TryParse(raw, out var ticks) ||
            ticks < DateTime.MinValue.Ticks ||
            ticks > DateTime.MaxValue.Ticks)
        {
            return false;
        }

        value = new DateTime(ticks, DateTimeKind.Utc);
        return true;
    }

    private static ValidationException InvalidCursor()
        => new(
            "Invalid cursor.",
            new Dictionary<string, string[]>
            {
                ["cursor"] = ["The cursor is malformed or from a different query."]
            });
}
