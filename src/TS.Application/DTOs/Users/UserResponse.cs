using TS.Domain.Entities;

namespace TS.Application.DTOs.Users;

public sealed class UserResponse
{
    public UserResponse(
        Guid id,
        string email,
        string? displayName,
        string role,
        bool isActive,
        DateTime createdAtUtc,
        DateTime? lastLoginAtUtc)
    {
        Id = id;
        Email = email;
        DisplayName = displayName;
        Role = role;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
        LastLoginAtUtc = lastLoginAtUtc;
    }

    public Guid Id { get; }

    public string Email { get; }

    public string? DisplayName { get; }

    public string Role { get; }

    public bool IsActive { get; }

    public DateTime CreatedAtUtc { get; }

    public DateTime? LastLoginAtUtc { get; }

    public static UserResponse From(User user)
        => new(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role.ToString().ToLowerInvariant(),
            user.IsActive,
            user.CreatedAtUtc,
            user.LastLoginAtUtc);
}
