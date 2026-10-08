using TS.Domain.Entities;

namespace TS.Application.DTOs.Admin;

public sealed class AdminUserResponse
{
    public AdminUserResponse(
        Guid id,
        string email,
        string? displayName,
        string role,
        bool isActive,
        DateTime createdAtUtc,
        DateTime? lastLoginAtUtc,
        int snippetCount,
        int activeRefreshTokenCount)
    {
        Id = id;
        Email = email;
        DisplayName = displayName;
        Role = role;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
        LastLoginAtUtc = lastLoginAtUtc;
        SnippetCount = snippetCount;
        ActiveRefreshTokenCount = activeRefreshTokenCount;
    }

    public Guid Id { get; }

    public string Email { get; }

    public string? DisplayName { get; }

    public string Role { get; }

    public bool IsActive { get; }

    public DateTime CreatedAtUtc { get; }

    public DateTime? LastLoginAtUtc { get; }

    public int SnippetCount { get; }

    public int ActiveRefreshTokenCount { get; }

    public static AdminUserResponse From(
        User user,
        int snippetCount,
        int activeRefreshTokenCount)
        => new(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role.ToString().ToLowerInvariant(),
            user.IsActive,
            user.CreatedAtUtc,
            user.LastLoginAtUtc,
            snippetCount,
            activeRefreshTokenCount);
}
