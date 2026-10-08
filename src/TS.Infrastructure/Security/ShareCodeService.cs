using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using TS.Application.Interfaces;

namespace TS.Infrastructure.Security;

public sealed class ShareCodeService : IShareCodeService
{
    // No 0/O, 1/I/l to keep codes human-typable.
    private const string Alphabet =
        "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

    private const int CodeLength = 16;

    private readonly Dictionary<short, byte[]> _keys;

    public ShareCodeService(IOptions<ShareCodeOptions> options)
    {
        _keys = [];

        foreach (var (version, key) in options.Value.Keys)
        {
            if (!short.TryParse(version, out var parsed) || parsed < 1)
            {
                throw new InvalidOperationException(
                    $"ShareCode key version '{version}' must be a positive integer.");
            }

            _keys[parsed] = Convert.FromBase64String(key);
        }

        if (_keys.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one ShareCode HMAC key must be configured.");
        }

        CurrentKeyVersion = _keys.Keys.Max();
        KeyVersions = _keys.Keys.OrderByDescending(v => v).ToArray();
    }

    public short CurrentKeyVersion { get; }

    public IReadOnlyList<short> KeyVersions { get; }

    public string GenerateCode()
    {
        var chars = new char[CodeLength];
        var index = 0;

        // Rejection sampling keeps the alphabet uniformly distributed.
        while (index < CodeLength)
        {
            var buffer = RandomNumberGenerator.GetBytes(CodeLength - index);

            foreach (var b in buffer)
            {
                if (b >= 248) // 62 * 4
                    continue;

                chars[index++] = Alphabet[b % Alphabet.Length];

                if (index == CodeLength)
                    break;
            }
        }

        return new string(chars);
    }

    public byte[] HashCode(string code, short keyVersion)
    {
        if (!_keys.TryGetValue(keyVersion, out var key))
        {
            throw new InvalidOperationException(
                $"No ShareCode HMAC key configured for version {keyVersion}.");
        }

        return HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(code));
    }
}
