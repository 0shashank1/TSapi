using System.ComponentModel.DataAnnotations;

namespace TS.Application.DTOs.ShareLinks;

/// <summary>
/// Partial update of link policy. Null properties are left unchanged.
/// The share credential itself is immutable: revoke and create a new
/// link instead.
/// </summary>
public sealed class UpdateShareLinkRequest
{
    public DateTime? ExpiresAtUtc { get; init; }

    [Range(1, int.MaxValue)]
    public int? MaxUses { get; init; }

    public bool ClearExpiresAtUtc { get; init; }

    public bool ClearMaxUses { get; init; }
}
