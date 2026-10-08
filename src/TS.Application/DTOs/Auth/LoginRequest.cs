using System.ComponentModel.DataAnnotations;

namespace TS.Application.DTOs.Auth;

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; init; } = null!;

    [Required]
    [MaxLength(128)]
    public string Password { get; init; } = null!;
}
