using System.ComponentModel.DataAnnotations;

namespace TS.Application.DTOs.Users;

public sealed class UpdateProfileRequest
{
    [Required]
    [MaxLength(100)]
    public string DisplayName { get; init; } = null!;
}
