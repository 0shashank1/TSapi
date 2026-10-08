using TS.Application.Common;
using TS.Application.DTOs.Users;
using TS.Application.Interfaces;

namespace TS.Infrastructure.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserResponse> GetMeAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);

        if (user is null)
            throw new NotFoundException("User not found.");

        return UserResponse.From(user);
    }

    public async Task<UserResponse> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);

        if (user is null)
            throw new NotFoundException("User not found.");

        user.UpdateProfile(request.DisplayName.Trim(), DateTime.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UserResponse.From(user);
    }

    public async Task ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);

        if (user is null)
            throw new NotFoundException("User not found.");

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedException("Current password is incorrect.");

        var now = DateTime.UtcNow;
        user.ChangePassword(_passwordHasher.Hash(request.NewPassword), now);

        var activeTokens = await _refreshTokens.ListActiveByUserAsync(
            userId,
            cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(now, "PasswordChanged", null);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);

        if (user is null)
            throw new NotFoundException("User not found.");

        var now = DateTime.UtcNow;
        user.Deactivate(now);

        var activeTokens = await _refreshTokens.ListActiveByUserAsync(
            userId,
            cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(now, "AccountDeactivated", null);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
