using System.ComponentModel.DataAnnotations;

namespace TS.Application.DTOs.ShareLinks;

public sealed class CreateShareLinkRequest
{
    public DateTime? ExpiresAtUtc { get; init; }

    [Range(1, int.MaxValue)]
    public int? MaxUses { get; init; }

    [MinLength(8)]
    [MaxLength(128)]
    public string? Password { get; init; }
}
