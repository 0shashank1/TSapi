using System.ComponentModel.DataAnnotations;

namespace TS.Application.DTOs.Snippets;

public sealed class CreateSnippetRequest
{
    [MaxLength(200)]
    public string? Title { get; init; }

    [Required]
    [MaxLength(1_048_576)]
    public string Content { get; init; } = null!;

    public DateTime? ExpiresAtUtc { get; init; }

    [Range(1, int.MaxValue)]
    public int? MaxViews { get; init; }
}
