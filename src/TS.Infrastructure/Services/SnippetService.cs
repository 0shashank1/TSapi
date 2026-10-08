using TS.Application.Common;
using TS.Application.DTOs.Snippets;
using TS.Application.Interfaces;
using TS.Domain.Entities;

namespace TS.Infrastructure.Services;

public sealed class SnippetService : ISnippetService
{
    private readonly ISnippetRepository _snippets;
    private readonly IUnitOfWork _unitOfWork;

    public SnippetService(
        ISnippetRepository snippets,
        IUnitOfWork unitOfWork)
    {
        _snippets = snippets;
        _unitOfWork = unitOfWork;
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

        _snippets.Add(snippet);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return SnippetResponse.From(snippet);
    }

    public async Task<PagedResponse<SnippetListItem>> ListAsync(
        AccessContext context,
        SnippetListQuery query,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var sortBy = ParseSortBy(query.SortBy);
        var status = ParseStatus(query.Status);
        var descending = ResolveDescending(sortBy, query.SortDirection);
        var cursor = ParseCursor(query.Cursor, sortBy);

        var rows = await _snippets.ListOwnedAsync(
            context.UserId,
            now,
            status,
            query.Search,
            sortBy,
            descending,
            cursor,
            query.PageSize + 1,
            cancellationToken);

        var hasMore = rows.Count > query.PageSize;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);

        string? nextCursor = null;
        if (hasMore && rows.Count > 0)
        {
            var last = rows[^1];
            nextCursor = sortBy switch
            {
                SnippetSortBy.Title => Cursor.Encode(last.Title ?? string.Empty, last.Id),
                SnippetSortBy.ViewCount => Cursor.Encode(last.ViewCount, last.Id),
                SnippetSortBy.UpdatedAt => Cursor.Encode(last.UpdatedAtUtc.Ticks, last.Id),
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
        var snippet = await _snippets.GetByIdAsNoTrackingAsync(
            snippetId,
            cancellationToken);

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
        var snippet = await _snippets.GetByIdAsync(snippetId, cancellationToken);

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

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return SnippetResponse.From(snippet);
    }

    public async Task DeleteAsync(
        AccessContext context,
        Guid snippetId,
        CancellationToken cancellationToken = default)
    {
        var snippet = await _snippets.GetByIdAsync(snippetId, cancellationToken);

        EnsureCanAccess(context, snippet);

        _snippets.Remove(snippet!);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
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

    private static SnippetSortBy ParseSortBy(string? sortBy)
    {
        var normalized = sortBy?.Trim().ToLowerInvariant();

        return normalized switch
        {
            "title" => SnippetSortBy.Title,
            "viewcount" => SnippetSortBy.ViewCount,
            "updatedat" => SnippetSortBy.UpdatedAt,
            _ => SnippetSortBy.CreatedAt
        };
    }

    private static SnippetStatus ParseStatus(string? status)
    {
        var normalized = status?.Trim().ToLowerInvariant();

        return normalized switch
        {
            null or "" or "all" => SnippetStatus.All,
            "active" => SnippetStatus.Active,
            "expired" => SnippetStatus.Expired,
            _ => throw new ValidationException(
                "Invalid status filter.",
                new Dictionary<string, string[]>
                {
                    ["status"] = ["Must be one of: active, expired, all."]
                })
        };
    }

    private static bool ResolveDescending(
        SnippetSortBy sortBy,
        string? direction)
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

        return sortBy is not SnippetSortBy.Title;
    }

    private static Keyset? ParseCursor(string? cursor, SnippetSortBy sortBy)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return null;

        return sortBy switch
        {
            SnippetSortBy.Title => ParseStringCursor(cursor),
            SnippetSortBy.ViewCount => ParseNumberCursor(cursor),
            _ => ParseDateCursor(cursor)
        };
    }

    private static Keyset ParseStringCursor(string cursor)
    {
        if (!Cursor.TryDecodeString(cursor, out var lastTitle, out var lastId))
            throw InvalidCursor();

        return new StringKeyset(lastTitle, lastId);
    }

    private static Keyset ParseNumberCursor(string cursor)
    {
        if (!Cursor.TryDecodeString(cursor, out var raw, out var lastId) ||
            !long.TryParse(raw, out var lastValue))
        {
            throw InvalidCursor();
        }

        return new NumberKeyset(lastValue, lastId);
    }

    private static Keyset ParseDateCursor(string cursor)
    {
        if (!Cursor.TryDecodeDateTime(cursor, out var lastValue, out var lastId))
            throw InvalidCursor();

        return new DateKeyset(lastValue, lastId);
    }

    private static ValidationException InvalidCursor()
        => new(
            "Invalid cursor.",
            new Dictionary<string, string[]>
            {
                ["cursor"] = ["The cursor is malformed or from a different query."]
            });
}
