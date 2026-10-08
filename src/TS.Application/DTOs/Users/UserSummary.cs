using TS.Domain.Entities;

namespace TS.Application.DTOs.Users;

public sealed class UserSummary
{
    public UserSummary(
        Guid id,
        string email,
        string? displayName,
        string role)
    {
        Id = id;
        Email = email;
        DisplayName = displayName;
        Role = role;
    }

    public Guid Id { get; }

    public string Email { get; }

    public string? DisplayName { get; }

    public string Role { get; }

    public static UserSummary From(User user)
        => new(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role.ToString().ToLowerInvariant());
}
