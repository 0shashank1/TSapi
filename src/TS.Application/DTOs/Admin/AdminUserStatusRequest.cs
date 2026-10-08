using System.ComponentModel.DataAnnotations;

namespace TS.Application.DTOs.Admin;

public sealed class AdminUserStatusRequest
{
    [Required]
    public bool IsActive { get; init; }
}
