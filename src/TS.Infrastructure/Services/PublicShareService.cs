using Microsoft.Extensions.Options;
using TS.Application.Common;
using TS.Application.DTOs.Public;
using TS.Application.Interfaces;
using TS.Domain.Entities;
using TS.Infrastructure.Security;

namespace TS.Infrastructure.Services;

public sealed class PublicShareService : IPublicShareService
{
    private readonly IShareLinkRepository _shareLinks;
    private readonly IShareAccessLogRepository _accessLogs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IShareCodeService _shareCodeService;
    private readonly IShareAccessTokenService _shareAccessTokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ShareCodeOptions _shareCodeOptions;

    public PublicShareService(
        IShareLinkRepository shareLinks,
        IShareAccessLogRepository accessLogs,
        IUnitOfWork unitOfWork,
        IShareCodeService shareCodeService,
        IShareAccessTokenService shareAccessTokenService,
        IPasswordHasher passwordHasher,
        IOptions<ShareCodeOptions> shareCodeOptions)
    {
        _shareLinks = shareLinks;
        _accessLogs = accessLogs;
        _unitOfWork = unitOfWork;
        _shareCodeService = shareCodeService;
        _shareAccessTokenService = shareAccessTokenService;
        _passwordHasher = passwordHasher;
        _shareCodeOptions = shareCodeOptions.Value;
    }

    public async Task<PublicShareResponse> GetAsync(
        string code,
        string? shareAccessToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var link = await FindByCodeAsync(code, cancellationToken);

        if (link is null)
        {
            await LogFailureAsync(null, ipAddress, userAgent, "NotFound", cancellationToken);
            throw new NotFoundException("Share not found.");
        }

        if (link.IsRevoked)
        {
            await LogFailureAsync(link.Id, ipAddress, userAgent, "Revoked", cancellationToken);
            throw new NotFoundException("Share not found.");
        }

        if (link.IsExpired(now))
        {
            await LogFailureAsync(link.Id, ipAddress, userAgent, "ExpiredLink", cancellationToken);
            throw new NotFoundException("Share not found.");
        }

        if (link.TextSnippet.IsExpired(now))
        {
            await LogFailureAsync(link.Id, ipAddress, userAgent, "ExpiredSnippet", cancellationToken);
            throw new NotFoundException("Share not found.");
        }

        if (link.HasReachedUseLimit())
        {
            await LogFailureAsync(link.Id, ipAddress, userAgent, "UseLimitExceeded", cancellationToken);
            throw new NotFoundException("Share not found.");
        }

        if (link.TextSnippet.HasReachedViewLimit())
        {
            await LogFailureAsync(link.Id, ipAddress, userAgent, "ViewLimitExceeded", cancellationToken);
            throw new NotFoundException("Share not found.");
        }

        if (link.IsPasswordProtected)
        {
            var unlocked = shareAccessToken is not null &&
                           _shareAccessTokenService
                               .Validate(shareAccessToken, link.Id)
                               .HasValue;

            if (!unlocked)
            {
                await LogFailureAsync(link.Id, ipAddress, userAgent, "PasswordRequired", cancellationToken);
                throw new UnauthorizedException(
                    "A password is required to access this share.",
                    "password-required");
            }
        }

        link.RecordAccess(now);
        link.TextSnippet.RecordView(now);

        _accessLogs.Add(new ShareAccessLog(
            link.Id,
            null,
            now,
            true,
            ipAddress,
            userAgent,
            null));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var snippet = link.TextSnippet;

        return new PublicShareResponse(
            snippet.Id,
            snippet.Title,
            snippet.Content,
            snippet.ExpiresAtUtc);
    }

    public async Task<UnlockResponse> UnlockAsync(
        string code,
        string password,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var link = await FindByCodeAsync(code, cancellationToken);

        if (link is null)
        {
            await LogFailureAsync(null, ipAddress, userAgent, "NotFound", cancellationToken);
            throw new NotFoundException("Share not found.");
        }

        if (link.IsRevoked || link.IsExpired(now))
        {
            await LogFailureAsync(link.Id, ipAddress, userAgent, "ExpiredLink", cancellationToken);
            throw new NotFoundException("Share not found.");
        }

        if (!link.IsPasswordProtected)
        {
            throw new ValidationException(
                "This share is not password protected.",
                new Dictionary<string, string[]>
                {
                    ["password"] = ["This share is not password protected."]
                });
        }

        if (!_passwordHasher.Verify(password, link.PasswordHash!))
        {
            await LogFailureAsync(link.Id, ipAddress, userAgent, "InvalidPassword", cancellationToken);
            throw new UnauthorizedException("Invalid password.");
        }

        var accessToken = _shareAccessTokenService.Generate(link.Id);
        var expiresAtUtc = now.AddMinutes(_shareCodeOptions.UnlockTokenMinutes);

        _accessLogs.Add(new ShareAccessLog(
            link.Id,
            null,
            now,
            true,
            ipAddress,
            userAgent,
            null));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UnlockResponse(accessToken, expiresAtUtc);
    }

    private async Task<ShareLink?> FindByCodeAsync(
        string code,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        // The link stores the key version it was created under, so
        // try each configured version (newest first).
        foreach (var version in _shareCodeService.KeyVersions)
        {
            var hash = _shareCodeService.HashCode(code, version);

            var link = await _shareLinks.FindByTokenAsync(
                version,
                hash,
                cancellationToken);

            if (link is not null)
                return link;
        }

        return null;
    }

    private async Task LogFailureAsync(
        Guid? shareLinkId,
        string? ipAddress,
        string? userAgent,
        string failureReason,
        CancellationToken cancellationToken)
    {
        _accessLogs.Add(new ShareAccessLog(
            shareLinkId,
            null,
            DateTime.UtcNow,
            false,
            ipAddress,
            userAgent,
            failureReason));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
