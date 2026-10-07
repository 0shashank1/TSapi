using System.Security.Cryptography;
using System.Text;
using TS.Application.Interfaces;

namespace TS.Infrastructure.Security;

public sealed class RefreshTokenGenerator
    : IRefreshTokenGenerator
{
    public string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);

        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    public byte[] Hash(string token)
    {
        return SHA256.HashData(
            Encoding.UTF8.GetBytes(token));
    }
}
