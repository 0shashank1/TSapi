using System.Security.Claims;
using Microsoft.Extensions.Options;
using TS.Application.Interfaces;
using TS.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;


namespace TS.Infrastructure.Security;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly byte[] _signingKey;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;

        _signingKey = Convert.FromBase64String(
            _options.SecretKey);

        if (_signingKey.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must be at least 256 bits.");
        }
    }

    public AccessToken GenerateAccessToken(User user)
    {
        var now = DateTime.UtcNow;
        var expiresAtUtc = now.AddMinutes(
            _options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,
                user.Id.ToString()),

            new(JwtRegisteredClaimNames.Email,
                user.Email),

            new("role",
                user.Role.ToString().ToLowerInvariant()),

            new(JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(_signingKey),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var written = new JwtSecurityTokenHandler()
            .WriteToken(token);

        return new AccessToken(written, expiresAtUtc);
    }
}
