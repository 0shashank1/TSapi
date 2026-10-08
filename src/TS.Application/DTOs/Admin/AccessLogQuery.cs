using System.ComponentModel.DataAnnotations;

namespace TS.Application.DTOs.Admin;

public sealed class AccessLogQuery
{
    public const int DefaultPageSize = 20;

    public const int MaxPageSize = 100;

    [Range(1, MaxPageSize)]
    public int PageSize { get; init; } = DefaultPageSize;

    public string? Cursor { get; init; }

    /// <summary>Filter on successful accesses.</summary>
    public bool? Success { get; init; }

    [MaxLength(100)]
    public string? Reason { get; init; }

    public DateTime? From { get; init; }

    public DateTime? To { get; init; }
}
