using TS.Domain.Entities;

namespace TS.Application.DTOs.ShareLinks;

public sealed class ShareLinkListItem
{
    public ShareLinkListItem(
        Guid id,
        DateTime? expiresAtUtc,
        int? maxUses,
        long useCount,
        bool isPasswordProtected,
        bool isRevoked,
        DateTime? lastAccessedAtUtc,
        DateTime createdAtUtc)
    {
        Id = id;
        ExpiresAtUtc = expiresAtUtc;
        MaxUses = maxUses;
        UseCount = useCount;
        IsPasswordProtected = isPasswordProtected;
        IsRevoked = isRevoked;
        LastAccessedAtUtc = lastAccessedAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public DateTime? ExpiresAtUtc { get; }

    public int? MaxUses { get; }

    public long UseCount { get; }

    public bool IsPasswordProtected { get; }

    public bool IsRevoked { get; }

    public DateTime? LastAccessedAtUtc { get; }

    public DateTime CreatedAtUtc { get; }

    public static ShareLinkListItem From(ShareLink link)
        => new(
            link.Id,
            link.ExpiresAtUtc,
            link.MaxUses,
            link.UseCount,
            link.IsPasswordProtected,
            link.IsRevoked,
            link.LastAccessedAtUtc,
            link.CreatedAtUtc);
}
