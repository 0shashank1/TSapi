using System.ComponentModel.DataAnnotations;

namespace TS.Application.DTOs.Users;

public sealed class ChangePasswordRequest
{
    [Required]
    [MaxLength(128)]
    public string CurrentPassword { get; init; } = null!;

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string NewPassword { get; init; } = null!;
}
