namespace TS.Application.DTOs.Public;

public sealed class UnlockResponse
{
    public UnlockResponse(string accessToken, DateTime expiresAtUtc)
    {
        AccessToken = accessToken;
        ExpiresAtUtc = expiresAtUtc;
    }

    public string AccessToken { get; }

    public DateTime ExpiresAtUtc { get; }
}
