using System.ComponentModel.DataAnnotations;

namespace TS.Application.DTOs.Snippets;

/// <summary>
/// Partial update. Null properties are left unchanged.
/// </summary>
public sealed class UpdateSnippetRequest
{
    [MaxLength(200)]
    public string? Title { get; init; }

    [MaxLength(1_048_576)]
    public string? Content { get; init; }

    public DateTime? ExpiresAtUtc { get; init; }

    [Range(1, int.MaxValue)]
    public int? MaxViews { get; init; }

    public bool ClearExpiresAtUtc { get; init; }

    public bool ClearMaxViews { get; init; }
}
