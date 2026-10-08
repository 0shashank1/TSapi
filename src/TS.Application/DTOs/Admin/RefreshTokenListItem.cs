using TS.Domain.Entities;

namespace TS.Application.DTOs.Admin;

public sealed class RefreshTokenListItem
{
    public RefreshTokenListItem(
        Guid id,
        Guid userId,
        Guid familyId,
        DateTime createdAtUtc,
        DateTime expiresAtUtc,
        DateTime? revokedAtUtc,
        string? revocationReason,
        string? createdByIp)
    {
        Id = id;
        UserId = userId;
        FamilyId = familyId;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        RevokedAtUtc = revokedAtUtc;
        RevocationReason = revocationReason;
        CreatedByIp = createdByIp;
    }

    public Guid Id { get; }

    public Guid UserId { get; }

    public Guid FamilyId { get; }

    public DateTime CreatedAtUtc { get; }

    public DateTime ExpiresAtUtc { get; }

    public DateTime? RevokedAtUtc { get; }

    public string? RevocationReason { get; }

    public string? CreatedByIp { get; }

    public static RefreshTokenListItem From(RefreshToken token)
        => new(
            token.Id,
            token.UserId,
            token.FamilyId,
            token.CreatedAtUtc,
            token.ExpiresAtUtc,
            token.RevokedAtUtc,
            token.RevocationReason,
            token.CreatedByIp);
}
