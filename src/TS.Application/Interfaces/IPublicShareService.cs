using TS.Application.DTOs.Public;

namespace TS.Application.Interfaces;

public interface IPublicShareService
{
    Task<PublicShareResponse> GetAsync(
        string code,
        string? shareAccessToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<UnlockResponse> UnlockAsync(
        string code,
        string password,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
}
