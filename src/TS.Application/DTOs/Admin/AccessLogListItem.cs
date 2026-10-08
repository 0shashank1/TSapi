using TS.Domain.Entities;

namespace TS.Application.DTOs.Admin;

public sealed class AccessLogListItem
{
    public AccessLogListItem(
        Guid id,
        Guid? shareLinkId,
        Guid? userId,
        DateTime accessedAtUtc,
        bool wasSuccessful,
        string? ipAddress,
        string? userAgent,
        string? failureReason)
    {
        Id = id;
        ShareLinkId = shareLinkId;
        UserId = userId;
        AccessedAtUtc = accessedAtUtc;
        WasSuccessful = wasSuccessful;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        FailureReason = failureReason;
    }

    public Guid Id { get; }

    public Guid? ShareLinkId { get; }

    public Guid? UserId { get; }

    public DateTime AccessedAtUtc { get; }

    public bool WasSuccessful { get; }

    public string? IpAddress { get; }

    public string? UserAgent { get; }

    public string? FailureReason { get; }

    public static AccessLogListItem From(ShareAccessLog log)
        => new(
            log.Id,
            log.ShareLinkId,
            log.UserId,
            log.AccessedAtUtc,
            log.WasSuccessful,
            log.IpAddress,
            log.UserAgent,
            log.FailureReason);
}
