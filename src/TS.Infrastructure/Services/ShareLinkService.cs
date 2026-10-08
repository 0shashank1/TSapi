using TS.Application.Common;
using TS.Application.DTOs.ShareLinks;
using TS.Application.Interfaces;
using TS.Domain.Entities;

namespace TS.Infrastructure.Services;

public sealed class ShareLinkService : IShareLinkService
{
    private readonly ISnippetRepository _snippets;
    private readonly IShareLinkRepository _shareLinks;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IShareCodeService _shareCodeService;
    private readonly IPasswordHasher _passwordHasher;

    public ShareLinkService(
        ISnippetRepository snippets,
        IShareLinkRepository shareLinks,
        IUnitOfWork unitOfWork,
        IShareCodeService shareCodeService,
        IPasswordHasher passwordHasher)
    {
        _snippets = snippets;
        _shareLinks = shareLinks;
        _unitOfWork = unitOfWork;
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
        var snippet = await _snippets.GetByIdAsync(snippetId, cancellationToken);

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

        _shareLinks.Add(link);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
        var snippet = await _snippets.GetByIdAsNoTrackingAsync(
            snippetId,
            cancellationToken);

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

        DateKeyset? keyset = null;
        if (!string.IsNullOrWhiteSpace(cursor))
        {
            if (!Cursor.TryDecodeDateTime(cursor, out var lastCreatedAt, out var lastId))
            {
                throw new ValidationException(
                    "Invalid cursor.",
                    new Dictionary<string, string[]>
                    {
                        ["cursor"] = ["The cursor is malformed or from a different query."]
                    });
            }

            keyset = new DateKeyset(lastCreatedAt, lastId);
        }

        var rows = await _shareLinks.ListBySnippetAsync(
            snippetId,
            keyset,
            pageSize + 1,
            cancellationToken);

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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ShareLinkResponse.From(link);
    }

    public async Task RevokeAsync(
        AccessContext context,
        Guid linkId,
        CancellationToken cancellationToken = default)
    {
        var link = await FindOwnedAsync(context, linkId, cancellationToken);

        link.Revoke(DateTime.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private const int SnippetPageSizeMax = 100;

    private async Task<ShareLink> FindOwnedAsync(
        AccessContext context,
        Guid linkId,
        CancellationToken cancellationToken)
    {
        var link = await _shareLinks.FindWithSnippetAsync(linkId, cancellationToken);

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
