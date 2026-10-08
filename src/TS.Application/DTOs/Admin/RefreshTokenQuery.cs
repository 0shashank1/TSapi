using System.ComponentModel.DataAnnotations;

namespace TS.Application.DTOs.Admin;

public sealed class RefreshTokenQuery
{
    public const int DefaultPageSize = 20;

    public const int MaxPageSize = 100;

    [Range(1, MaxPageSize)]
    public int PageSize { get; init; } = DefaultPageSize;

    public string? Cursor { get; init; }

    public Guid? UserId { get; init; }

    /// <summary>active | revoked | all</summary>
    [MaxLength(10)]
    public string? Status { get; init; }
}
