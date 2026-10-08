using Microsoft.EntityFrameworkCore;
using TS.Application.Common;
using TS.Application.DTOs.ShareLinks;
using TS.Application.Interfaces;
using TS.Domain.Entities;
using TS.Infrastructure.Persistence;

namespace TS.Infrastructure.Services;

public sealed class ShareLinkService : IShareLinkService
{
    private readonly TSDbContext _db;
    private readonly IShareCodeService _shareCodeService;
    private readonly IPasswordHasher _passwordHasher;

    public ShareLinkService(
        TSDbContext db,
        IShareCodeService shareCodeService,
        IPasswordHasher passwordHasher)
    {
        _db = db;
        _shareCodeService = shareCodeService;
        _passwordHasher = passwordHasher;
    }

    public async Task<ShareLinkResponse> CreateAsync(
        AccessContext context,
        Guid snippetId,
        CreateShareLinkRequest request,
        string baseUrl,
        CancellationToken cancellationToken = default)
    {
        var snippet = await _db.TextSnippets
            .FirstOrDefaultAsync(s => s.Id == snippetId, cancellationToken);

        EnsureCanAccess(context, snippet);

        var now = DateTime.UtcNow;

        if (request.ExpiresAtUtc.HasValue &&
            request.ExpiresAtUtc.Value <= now)
        {
            throw new ValidationException(
                "Expiration must be in the future.",
                new Dictionary<string, string[]>
                {
                    ["expiresAtUtc"] = ["Expiration must be in the future."]
                });
        }

        var code = _shareCodeService.GenerateCode();
        var keyVersion = _shareCodeService.CurrentKeyVersion;

        var link = new ShareLink(
            snippet!.Id,
            _shareCodeService.HashCode(code, keyVersion),
            keyVersion,
            request.Password is null
                ? null
                : _passwordHasher.Hash(request.Password),
            request.ExpiresAtUtc,
            request.MaxUses);

        _db.ShareLinks.Add(link);
        await _db.SaveChangesAsync(cancellationToken);

        var url = $"{baseUrl.TrimEnd('/')}/s/{code}";

        return ShareLinkResponse.From(link, url);
    }

    public async Task<PagedResponse<ShareLinkListItem>> ListForSnippetAsync(
        AccessContext context,
        Guid snippetId,
        int pageSize,
        string? cursor,
        CancellationToken cancellationToken = default)
    {
        var snippet = await _db.TextSnippets
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == snippetId, cancellationToken);

        EnsureCanAccess(context, snippet);

        if (pageSize < 1 || pageSize > SnippetPageSizeMax)
        {
            throw new ValidationException(
                "Invalid page size.",
                new Dictionary<string, string[]>
                {
                    ["pageSize"] = [$"Must be between 1 and {SnippetPageSizeMax}."]
                });
        }

        IQueryable<ShareLink> source = _db.ShareLinks.AsNoTracking()
            .Where(l => l.TextSnippetId == snippetId);

        if (!string.IsNullOrWhiteSpace(cursor))
        {
            if (!Cursor.TryDecodeString(cursor, out var rawTicks, out var lastId) ||
                !long.TryParse(rawTicks, out var ticks) ||
                ticks < DateTime.MinValue.Ticks ||
                ticks > DateTime.MaxValue.Ticks)
            {
                throw new ValidationException(
                    "Invalid cursor.",
                    new Dictionary<string, string[]>
                    {
                        ["cursor"] = ["The cursor is malformed or from a different query."]
                    });
            }

            var lastCreatedAt = new DateTime(ticks, DateTimeKind.Utc);
            source = source.Where(l =>
                l.CreatedAtUtc < lastCreatedAt ||
                (l.CreatedAtUtc == lastCreatedAt && l.Id.CompareTo(lastId) < 0));
        }

        var rows = await source
            .OrderByDescending(l => l.CreatedAtUtc)
            .ThenByDescending(l => l.Id)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > pageSize;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);

        string? nextCursor = null;
        if (hasMore && rows.Count > 0)
        {
            var last = rows[^1];
            nextCursor = Cursor.Encode(last.CreatedAtUtc.Ticks, last.Id);
        }

        return new PagedResponse<ShareLinkListItem>(
            rows.Select(ShareLinkListItem.From).ToList(),
            nextCursor);
    }

    public async Task<ShareLinkResponse> GetAsync(
        AccessContext context,
        Guid linkId,
        CancellationToken cancellationToken = default)
    {
        var link = await FindOwnedAsync(context, linkId, cancellationToken);

        return ShareLinkResponse.From(link);
    }

    public async Task<ShareLinkResponse> UpdateAsync(
        AccessContext context,
        Guid linkId,
        UpdateShareLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        var link = await FindOwnedAsync(context, linkId, cancellationToken);

        var expiresAtUtc = request.ClearExpiresAtUtc
            ? null
            : request.ExpiresAtUtc ?? link.ExpiresAtUtc;

        var maxUses = request.ClearMaxUses
            ? null
            : request.MaxUses ?? link.MaxUses;

        if (expiresAtUtc.HasValue && expiresAtUtc.Value <= link.CreatedAtUtc)
        {
            throw new ValidationException(
                "Expiration must be in the future.",
                new Dictionary<string, string[]>
                {
                    ["expiresAtUtc"] = ["Expiration must be after the link was created."]
                });
        }

        link.UpdatePolicy(expiresAtUtc, maxUses, DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        return ShareLinkResponse.From(link);
    }

    public async Task RevokeAsync(
        AccessContext context,
        Guid linkId,
        CancellationToken cancellationToken = default)
    {
        var link = await FindOwnedAsync(context, linkId, cancellationToken);

        link.Revoke(DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private const int SnippetPageSizeMax = 100;

    private async Task<ShareLink> FindOwnedAsync(
        AccessContext context,
        Guid linkId,
        CancellationToken cancellationToken)
    {
        var link = await _db.ShareLinks
            .Include(l => l.TextSnippet)
            .FirstOrDefaultAsync(l => l.Id == linkId, cancellationToken);

        if (link is null)
            throw new NotFoundException("Share link not found.");

        if (link.TextSnippet.OwnerUserId != context.UserId && !context.IsAdmin)
            throw new ForbiddenException("You do not own this share link.");

        return link;
    }

    private static void EnsureCanAccess(
        AccessContext context,
        TextSnippet? snippet)
    {
        if (snippet is null)
            throw new NotFoundException("Snippet not found.");

        if (snippet.OwnerUserId != context.UserId && !context.IsAdmin)
            throw new ForbiddenException("You do not own this snippet.");
    }
}
