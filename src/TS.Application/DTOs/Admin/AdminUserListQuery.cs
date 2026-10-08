using System.ComponentModel.DataAnnotations;

namespace TS.Application.DTOs.Admin;

public sealed class AdminUserListQuery
{
    public const int DefaultPageSize = 20;

    public const int MaxPageSize = 100;

    [Range(1, MaxPageSize)]
    public int PageSize { get; init; } = DefaultPageSize;

    public string? Cursor { get; init; }

    [MaxLength(320)]
    public string? Search { get; init; }

    /// <summary>active | inactive | all</summary>
    [MaxLength(10)]
    public string? Status { get; init; }

    /// <summary>user | admin</summary>
    [MaxLength(10)]
    public string? Role { get; init; }
}
