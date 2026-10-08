using System.ComponentModel.DataAnnotations;

namespace TS.Application.DTOs.Snippets;

public sealed class SnippetListQuery
{
    public const int DefaultPageSize = 20;

    public const int MaxPageSize = 100;

    [Range(1, MaxPageSize)]
    public int PageSize { get; init; } = DefaultPageSize;

    public string? Cursor { get; init; }

    [MaxLength(200)]
    public string? Search { get; init; }

    /// <summary>createdAt | title | viewCount | updatedAt</summary>
    [MaxLength(20)]
    public string? SortBy { get; init; }

    /// <summary>asc | desc</summary>
    [MaxLength(4)]
    public string? SortDirection { get; init; }

    /// <summary>active | expired | all</summary>
    [MaxLength(10)]
    public string? Status { get; init; }
}
