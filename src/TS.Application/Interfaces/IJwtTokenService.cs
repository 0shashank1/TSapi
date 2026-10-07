
using TS.Domain.Entities;

namespace TS.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
}
