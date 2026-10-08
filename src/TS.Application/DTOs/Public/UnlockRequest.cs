using System.ComponentModel.DataAnnotations;

namespace TS.Application.DTOs.Public;

public sealed class UnlockRequest
{
    [Required]
    [MaxLength(128)]
    public string Password { get; init; } = null!;
}
