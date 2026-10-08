using System.ComponentModel.DataAnnotations;

namespace TS.Application.DTOs.Auth;

public sealed class RefreshRequest
{
    [Required]
    public string RefreshToken { get; init; } = null!;
}
