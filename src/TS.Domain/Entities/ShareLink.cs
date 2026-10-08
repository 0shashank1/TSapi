using TS.Domain.Common;

namespace TS.Domain.Entities;

public sealed class ShareLink : EntityBase
{
    private ShareLink()
    {
        // EF Core constructor
    }

    public ShareLink(
        Guid textSnippetId,
        byte[] tokenHash,
        short tokenKeyVersion,
        string? passwordHash,
        DateTime? expiresAtUtc,
        int? maxUses)
    {
        TextSnippetId = textSnippetId;
        TokenHash = tokenHash;
        TokenKeyVersion = tokenKeyVersion;
        PasswordHash = passwordHash;
        ExpiresAtUtc = expiresAtUtc;
        MaxUses = maxUses;
        UseCount = 0;
    }

    public Guid TextSnippetId { get; private set; }

    /// <summary>
    /// HMAC-SHA256(code) result.
    /// Raw share code is never stored.
    /// </summary>
    public byte[] TokenHash { get; private set; } = [];

    /// <summary>
    /// Allows HMAC secret rotation without invalidating
    /// every existing share link immediately.
    /// </summary>
    public short TokenKeyVersion { get; private set; }

    /// <summary>
    /// BCrypt hash when password protection is enabled.
    /// </summary>
    public string? PasswordHash { get; private set; }

    public DateTime? ExpiresAtUtc { get; private set; }

    public int? MaxUses { get; private set; }

    public long UseCount { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public DateTime? LastAccessedAtUtc { get; private set; }

    public TextSnippet TextSnippet { get; private set; } = null!;

    public bool IsPasswordProtected =>
        !string.IsNullOrWhiteSpace(PasswordHash);

    public bool IsRevoked =>
        RevokedAtUtc.HasValue;

    public bool IsExpired(DateTime utcNow)
    {
        return ExpiresAtUtc.HasValue &&
               ExpiresAtUtc.Value <= utcNow;
    }

    public bool HasReachedUseLimit()
    {
        return MaxUses.HasValue &&
               UseCount >= MaxUses.Value;
    }

    public bool IsAccessible(DateTime utcNow)
    {
        return !IsRevoked &&
               !IsExpired(utcNow) &&
               !HasReachedUseLimit() &&
               !TextSnippet.IsExpired(utcNow) &&
               !TextSnippet.HasReachedViewLimit();
    }

    public void RecordAccess(DateTime utcNow)
    {
        UseCount++;
        LastAccessedAtUtc = utcNow;

        Touch(utcNow);
    }

    public ICollection<ShareAccessLog> AccessLogs { get; private set; }
        = new List<ShareAccessLog>();

    public void Revoke(DateTime utcNow)
    {
        RevokedAtUtc = utcNow;

        Touch(utcNow);
    }

    public void UpdatePolicy(
        DateTime? expiresAtUtc,
        int? maxUses,
        DateTime utcNow)
    {
        ExpiresAtUtc = expiresAtUtc;
        MaxUses = maxUses;

        Touch(utcNow);
    }
}
