using System.ComponentModel.DataAnnotations;

namespace TS.Application.DTOs.Admin;

public sealed class AdminUserRoleRequest
{
    /// <summary>user | admin</summary>
    [Required]
    [MaxLength(10)]
    public string Role { get; init; } = null!;
}
