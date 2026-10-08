using System.Text.Json.Serialization;
using TS.Domain.Entities;

namespace TS.Application.DTOs.ShareLinks;

public sealed class ShareLinkResponse
{
    public ShareLinkResponse(
        Guid id,
        Guid snippetId,
        string? url,
        DateTime? expiresAtUtc,
        int? maxUses,
        long useCount,
        bool isPasswordProtected,
        bool isRevoked,
        DateTime? lastAccessedAtUtc,
        DateTime createdAtUtc)
    {
        Id = id;
        SnippetId = snippetId;
        Url = url;
        ExpiresAtUtc = expiresAtUtc;
        MaxUses = maxUses;
        UseCount = useCount;
        IsPasswordProtected = isPasswordProtected;
        IsRevoked = isRevoked;
        LastAccessedAtUtc = lastAccessedAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public Guid SnippetId { get; }

    /// <summary>
    /// Only populated at creation time: the raw share code is never
    /// stored, so the URL cannot be reconstructed later.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; }

    public DateTime? ExpiresAtUtc { get; }

    public int? MaxUses { get; }

    public long UseCount { get; }

    public bool IsPasswordProtected { get; }

    public bool IsRevoked { get; }

    public DateTime? LastAccessedAtUtc { get; }

    public DateTime CreatedAtUtc { get; }

    public static ShareLinkResponse From(ShareLink link, string? url = null)
        => new(
            link.Id,
            link.TextSnippetId,
            url,
            link.ExpiresAtUtc,
            link.MaxUses,
            link.UseCount,
            link.IsPasswordProtected,
            link.IsRevoked,
            link.LastAccessedAtUtc,
            link.CreatedAtUtc);
}
