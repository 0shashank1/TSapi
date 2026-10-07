using TS.Domain.Common;

namespace TS.Domain.Entities;

public sealed class RefreshToken : EntityBase
{
    private RefreshToken()
    {
    }

    public RefreshToken(
        Guid userId,
        byte[] tokenHash,
        Guid familyId,
        DateTime expiresAtUtc,
        string? createdByIp,
        string? userAgent)
    {
        UserId = userId;
        TokenHash = tokenHash;
        FamilyId = familyId;
        ExpiresAtUtc = expiresAtUtc;
        CreatedByIp = createdByIp;
        UserAgent = userAgent;
    }

    public Guid UserId { get; private set; }

    /// <summary>
    /// SHA-256 hash of the random refresh token.
    /// The plaintext token is never stored.
    /// </summary>
    public byte[] TokenHash { get; private set; } = [];

    /// <summary>
    /// Identifies a login/session token family.
    /// </summary>
    public Guid FamilyId { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public string? CreatedByIp { get; private set; }

    public string? UserAgent { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public string? RevokedByIp { get; private set; }

    public string? RevocationReason { get; private set; }

    /// <summary>
    /// Audit reference to the replacement token.
    /// </summary>
    public Guid? ReplacedByTokenId { get; private set; }

    public User User { get; private set; } = null!;

    public bool IsExpired(DateTime utcNow)
        => ExpiresAtUtc <= utcNow;

    public bool IsRevoked
        => RevokedAtUtc.HasValue;

    public bool IsActive(DateTime utcNow)
        => !IsRevoked && !IsExpired(utcNow);

    public void Revoke(
        DateTime utcNow,
        string reason,
        string? ipAddress,
        Guid? replacementTokenId = null)
    {
        if (IsRevoked)
            return;

        RevokedAtUtc = utcNow;
        RevocationReason = reason;
        RevokedByIp = ipAddress;
        ReplacedByTokenId = replacementTokenId;

        Touch(utcNow);
    }
}
