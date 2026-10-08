using System.ComponentModel.DataAnnotations;

namespace TS.Application.DTOs.Auth;

public sealed class LogoutRequest
{
    [Required]
    public string RefreshToken { get; init; } = null!;
}
