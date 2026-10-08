using TS.Domain.Entities;

namespace TS.Application.DTOs.Admin;

public sealed class AdminUserListItem
{
    public AdminUserListItem(
        Guid id,
        string email,
        string? displayName,
        string role,
        bool isActive,
        DateTime createdAtUtc)
    {
        Id = id;
        Email = email;
        DisplayName = displayName;
        Role = role;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public string Email { get; }

    public string? DisplayName { get; }

    public string Role { get; }

    public bool IsActive { get; }

    public DateTime CreatedAtUtc { get; }

    public static AdminUserListItem From(User user)
        => new(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role.ToString().ToLowerInvariant(),
            user.IsActive,
            user.CreatedAtUtc);
}
