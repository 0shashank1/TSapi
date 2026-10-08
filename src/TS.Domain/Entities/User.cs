using TS.Domain.Common;
using TS.Domain.Enums;

namespace TS.Domain.Entities;

public sealed class User : EntityBase
{
    private User()
    {
    }

    public User(
        string email,
        string normalizedEmail,
        string passwordHash,
        string? displayName)
    {
        Email = email;
        NormalizedEmail = normalizedEmail;
        PasswordHash = passwordHash;
        DisplayName = displayName;
        Role = UserRole.User;
        IsActive = true;
    }

    public string Email { get; private set; } = null!;

    public string NormalizedEmail { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public string? DisplayName { get; private set; }

    public UserRole Role { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime? LastLoginAtUtc { get; private set; }

    public ICollection<TextSnippet> Snippets { get; private set; }
        = new List<TextSnippet>();

    public ICollection<RefreshToken> RefreshTokens { get; private set; }
        = new List<RefreshToken>();

    public ICollection<ShareAccessLog> AccessLogs { get; private set; }
        = new List<ShareAccessLog>();

    public void RecordLogin(DateTime utcNow)
    {
        LastLoginAtUtc = utcNow;
        Touch(utcNow);
    }

    public void PromoteToAdmin(DateTime utcNow)
    {
        Role = UserRole.Admin;
        Touch(utcNow);
    }

    public void Deactivate(DateTime utcNow)
    {
        IsActive = false;
        Touch(utcNow);
    }

    public void Activate(DateTime utcNow)
    {
        IsActive = true;
        Touch(utcNow);
    }

    public void UpdateProfile(string? displayName, DateTime utcNow)
    {
        DisplayName = displayName;
        Touch(utcNow);
    }

    public void ChangePassword(string newPasswordHash, DateTime utcNow)
    {
        PasswordHash = newPasswordHash;
        Touch(utcNow);
    }

    public void SetRole(UserRole role, DateTime utcNow)
    {
        Role = role;
        Touch(utcNow);
    }
}
