using System.Text;
using System.Text.Json;

namespace TS.Application.Common;

/// <summary>
/// Opaque keyset cursor. Payload is a small JSON array whose first
/// element is the last sort value (ticks or raw string) and whose
/// second element is the last row id, base64url encoded so clients
/// treat it as opaque.
/// </summary>
public static class Cursor
{
    public static string Encode(long sortValueTicks, Guid id)
        => EncodeInternal(JsonSerializer.Serialize(new object[] { sortValueTicks, id }));

    public static string Encode(string sortValue, Guid id)
        => EncodeInternal(JsonSerializer.Serialize(new object[] { sortValue, id }));

    public static bool TryDecodeDateTime(
        string? cursor,
        out DateTime sortValue,
        out Guid id)
    {
        sortValue = default;
        id = default;

        if (!TryDecodeInternal(cursor, out var parts) || parts.Length != 2)
            return false;

        if (!long.TryParse(parts[0], out var ticks) ||
            ticks < DateTime.MinValue.Ticks ||
            ticks > DateTime.MaxValue.Ticks ||
            !Guid.TryParse(parts[1], out id))
        {
            return false;
        }

        sortValue = new DateTime(ticks, DateTimeKind.Utc);
        return true;
    }

    public static bool TryDecodeString(
        string? cursor,
        out string sortValue,
        out Guid id)
    {
        sortValue = string.Empty;
        id = default;

        if (!TryDecodeInternal(cursor, out var parts) || parts.Length != 2)
            return false;

        if (!Guid.TryParse(parts[1], out id))
            return false;

        sortValue = parts[0];
        return true;
    }

    private static string EncodeInternal(string json)
        => Base64UrlEncode(Encoding.UTF8.GetBytes(json));

    private static bool TryDecodeInternal(string? cursor, out string[] parts)
    {
        parts = [];

        if (string.IsNullOrWhiteSpace(cursor))
            return false;

        try
        {
            var json = Encoding.UTF8.GetString(Base64UrlDecode(cursor));
            using var document = JsonDocument.Parse(json);

            if (document.RootElement.ValueKind != JsonValueKind.Array ||
                document.RootElement.GetArrayLength() != 2)
            {
                return false;
            }

            var array = document.RootElement.EnumerateArray();
            var first = array.MoveNext() ? array.Current : default;
            var second = array.MoveNext() ? array.Current : default;

            if (first.ValueKind != JsonValueKind.Number &&
                first.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            if (second.ValueKind != JsonValueKind.String)
                return false;

            parts = [first.ToString(), second.ToString()];
            return true;
        }
        catch (Exception ex) when (ex is FormatException or JsonException or InvalidOperationException)
        {
            return false;
        }
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2:
                s += "==";
                break;
            case 3:
                s += "=";
                break;
        }

        return Convert.FromBase64String(s);
    }
}
