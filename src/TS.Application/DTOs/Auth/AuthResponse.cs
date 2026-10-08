using TS.Application.DTOs.Users;

namespace TS.Application.DTOs.Auth;

public sealed class AuthResponse
{
    public AuthResponse(
        UserSummary user,
        string accessToken,
        DateTime accessTokenExpiresAtUtc,
        string refreshToken)
    {
        User = user;
        AccessToken = accessToken;
        AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc;
        RefreshToken = refreshToken;
    }

    public UserSummary User { get; }

    public string AccessToken { get; }

    public DateTime AccessTokenExpiresAtUtc { get; }

    public string RefreshToken { get; }
}
