using Microsoft.EntityFrameworkCore;
using TS.Application.Common;
using TS.Application.DTOs.Users;
using TS.Application.Interfaces;
using TS.Infrastructure.Persistence;

namespace TS.Infrastructure.Services;

public sealed class UserService : IUserService
{
    private readonly TSDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(TSDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserResponse> GetMeAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FindAsync([userId], cancellationToken);

        if (user is null)
            throw new NotFoundException("User not found.");

        return UserResponse.From(user);
    }

    public async Task<UserResponse> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FindAsync([userId], cancellationToken);

        if (user is null)
            throw new NotFoundException("User not found.");

        user.UpdateProfile(request.DisplayName.Trim(), DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        return UserResponse.From(user);
    }

    public async Task ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FindAsync([userId], cancellationToken);

        if (user is null)
            throw new NotFoundException("User not found.");

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedException("Current password is incorrect.");

        var now = DateTime.UtcNow;
        user.ChangePassword(_passwordHasher.Hash(request.NewPassword), now);

        var activeTokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(now, "PasswordChanged", null);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FindAsync([userId], cancellationToken);

        if (user is null)
            throw new NotFoundException("User not found.");

        var now = DateTime.UtcNow;
        user.Deactivate(now);

        var activeTokens = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(now, "AccountDeactivated", null);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
