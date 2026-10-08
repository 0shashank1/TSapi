using TS.Domain.Entities;

namespace TS.Application.Interfaces;

public sealed record AccessToken(string Token, DateTime ExpiresAtUtc);

public interface IJwtTokenService
{
    AccessToken GenerateAccessToken(User user);
}
