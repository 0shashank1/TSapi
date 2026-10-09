namespace TS.Infrastructure.BackgroundJobs;

/// <summary>
/// Settings for <see cref="ExpiredContentCleanupService"/>. Bound from the
/// "Cleanup" configuration section; every value has a code default, so the
/// section may be omitted entirely.
/// </summary>
public sealed class CleanupOptions
{
    public const string SectionName = "Cleanup";

    /// <summary>Master switch. When false the host still starts but idles.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Delay between cleanup passes. Minimum 1 minute.</summary>
    public int IntervalMinutes { get; set; } = 60;

    /// <summary>Rows deleted per statement. Minimum 1.</summary>
    public int BatchSize { get; set; } = 500;

    /// <summary>Days an expired snippet stays visible before it is purged.</summary>
    public int SnippetRetentionDays { get; set; } = 30;

    /// <summary>Days an expired (or revoked) share link is kept before it is purged.</summary>
    public int ShareLinkRetentionDays { get; set; } = 30;

    /// <summary>Days an expired refresh token is kept (reuse-detection evidence).</summary>
    public int RefreshTokenRetentionDays { get; set; } = 7;

    /// <summary>Days an access-log row is kept for the admin audit views.</summary>
    public int AccessLogRetentionDays { get; set; } = 90;
}
