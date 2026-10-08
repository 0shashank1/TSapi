using TS.Application.DTOs.Users;

namespace TS.Application.Interfaces;

public interface IUserService
{
    Task<UserResponse> GetMeAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<UserResponse> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default);

    Task DeactivateAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
