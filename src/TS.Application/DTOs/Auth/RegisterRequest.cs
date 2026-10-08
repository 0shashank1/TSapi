using System.ComponentModel.DataAnnotations;

namespace TS.Application.DTOs.Auth;

public sealed class RegisterRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; init; } = null!;

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string Password { get; init; } = null!;

    [MaxLength(100)]
    public string? DisplayName { get; init; }
}
