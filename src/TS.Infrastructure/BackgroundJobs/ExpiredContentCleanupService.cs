using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TS.Application.Interfaces;

namespace TS.Infrastructure.BackgroundJobs;

/// <summary>
/// Hosted background job that purges content once it is past its retention
/// window: expired snippets, expired/revoked share links, expired refresh
/// tokens and old access logs. Every category goes through its repository in
/// batches, so no pass ever issues one unbounded delete.
/// </summary>
public sealed class ExpiredContentCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<CleanupOptions> _options;
    private readonly ILogger<ExpiredContentCleanupService> _logger;

    public ExpiredContentCleanupService(
        IServiceScopeFactory scopeFactory,
        IOptions<CleanupOptions> options,
        ILogger<ExpiredContentCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    private CleanupOptions Options => _options.Value;

    private TimeSpan Interval =>
        TimeSpan.FromMinutes(Math.Max(1, Options.IntervalMinutes));

    private int BatchSize => Math.Max(1, Options.BatchSize);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Options.Enabled)
        {
            _logger.LogInformation(
                "Expired content cleanup is disabled by configuration.");
            return;
        }

        _logger.LogInformation(
            "Expired content cleanup started (every {IntervalMinutes} min, batch size {BatchSize}).",
            Interval.TotalMinutes,
            BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Expired content cleanup pass failed.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Expired content cleanup stopped.");
    }

    /// <summary>
    /// Runs a single cleanup pass. Public so the job can be triggered (or
    /// asserted on) without waiting for the timer.
    /// </summary>
    public async Task<CleanupSummary> RunOnceAsync(
        CancellationToken cancellationToken = default)
    {
        var options = Options;
        var now = DateTime.UtcNow;

        using var scope = _scopeFactory.CreateScope();
        var provider = scope.ServiceProvider;

        var snippets = provider.GetRequiredService<ISnippetRepository>();
        var shareLinks = provider.GetRequiredService<IShareLinkRepository>();
        var refreshTokens = provider.GetRequiredService<IRefreshTokenRepository>();
        var accessLogs = provider.GetRequiredService<IShareAccessLogRepository>();

        var summary = new CleanupSummary(
            await PurgeBatchedAsync(
                snippets.PurgeExpiredAsync,
                Cutoff(now, options.SnippetRetentionDays),
                BatchSize,
                cancellationToken),
            await PurgeBatchedAsync(
                shareLinks.PurgeExpiredAsync,
                Cutoff(now, options.ShareLinkRetentionDays),
                BatchSize,
                cancellationToken),
            await PurgeBatchedAsync(
                refreshTokens.PurgeExpiredAsync,
                Cutoff(now, options.RefreshTokenRetentionDays),
                BatchSize,
                cancellationToken),
            await PurgeBatchedAsync(
                accessLogs.PurgeOlderThanAsync,
                Cutoff(now, options.AccessLogRetentionDays),
                BatchSize,
                cancellationToken));

        if (summary.Total > 0)
        {
            _logger.LogInformation(
                "Expired content cleanup removed {Snippets} snippets, {ShareLinks} share links, {RefreshTokens} refresh tokens and {AccessLogs} access logs.",
                summary.Snippets,
                summary.ShareLinks,
                summary.RefreshTokens,
                summary.AccessLogs);
        }
        else
        {
            _logger.LogDebug("Expired content cleanup: nothing past retention.");
        }

        return summary;
    }

    private static async Task<int> PurgeBatchedAsync(
        Func<DateTime, int, CancellationToken, Task<int>> purge,
        DateTime cutoff,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var total = 0;

        while (true)
        {
            var deleted = await purge(cutoff, batchSize, cancellationToken);
            total += deleted;

            if (deleted < batchSize)
                return total;
        }
    }

    private static DateTime Cutoff(DateTime now, int retentionDays)
        => now.AddDays(-Math.Max(0, retentionDays));
}

/// <summary>Rows removed by one cleanup pass.</summary>
public sealed record CleanupSummary(
    int Snippets,
    int ShareLinks,
    int RefreshTokens,
    int AccessLogs)
{
    public int Total => Snippets + ShareLinks + RefreshTokens + AccessLogs;
}
