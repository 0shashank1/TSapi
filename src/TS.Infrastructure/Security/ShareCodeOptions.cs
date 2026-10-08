namespace TS.Infrastructure.Security;

public sealed class ShareCodeOptions
{
    public const string SectionName = "ShareCode";

    /// <summary>
    /// HMAC keys keyed by version number (as a string, e.g. "1").
    /// Rotation: add a new higher-numbered key; the highest version
    /// becomes current for new links while older keys keep old links
    /// resolvable.
    /// </summary>
    public Dictionary<string, string> Keys { get; init; } = [];

    /// <summary>Lifetime of the short-lived share access token.</summary>
    public int UnlockTokenMinutes { get; init; } = 10;
}
