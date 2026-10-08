using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TS.Application.Interfaces;

namespace TS.Infrastructure.Security;

public sealed class ShareAccessTokenService : IShareAccessTokenService
{
    public const string Audience = "TS.Share";
    public const string PurposeClaim = "purpose";

    private readonly JwtOptions _jwtOptions;
    private readonly ShareCodeOptions _shareCodeOptions;
    private readonly SymmetricSecurityKey _signingKey;

    public ShareAccessTokenService(
        IOptions<JwtOptions> jwtOptions,
        IOptions<ShareCodeOptions> shareCodeOptions)
    {
        _jwtOptions = jwtOptions.Value;
        _shareCodeOptions = shareCodeOptions.Value;
        _signingKey = new SymmetricSecurityKey(
            Convert.FromBase64String(_jwtOptions.SecretKey));
    }

    public string Generate(Guid shareLinkId)
    {
        var now = DateTime.UtcNow;
        var expiresAtUtc = now.AddMinutes(
            _shareCodeOptions.UnlockTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, shareLinkId.ToString()),
            new(PurposeClaim, "share-access"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAtUtc,
            signingCredentials: new SigningCredentials(
                _signingKey, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public Guid? Validate(string token, Guid shareLinkId)
    {
        try
        {
            var principal = new JwtSecurityTokenHandler
            {
                MapInboundClaims = false
            }.ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _signingKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                },
                out _);

            var purpose = principal.FindFirst(PurposeClaim)?.Value;
            var subject = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (purpose != "share-access" ||
                !Guid.TryParse(subject, out var linkId) ||
                linkId != shareLinkId)
            {
                return null;
            }

            return linkId;
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }
}
