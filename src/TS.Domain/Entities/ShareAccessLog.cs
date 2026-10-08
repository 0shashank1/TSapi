using TS.Domain.Common;

namespace TS.Domain.Entities;

public sealed class ShareAccessLog : IEntity
{
    private ShareAccessLog()
    {
    }

    public ShareAccessLog(
        Guid? shareLinkId,
        Guid? userId,
        DateTime accessedAtUtc,
        bool wasSuccessful,
        string? ipAddress,
        string? userAgent,
        string? failureReason)
    {
        Id = Guid.NewGuid();
        ShareLinkId = shareLinkId;
        UserId = userId;
        AccessedAtUtc = accessedAtUtc;
        WasSuccessful = wasSuccessful;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        FailureReason = failureReason;
    }

    public Guid Id { get; private set; }

    public Guid? ShareLinkId { get; private set; }

    public Guid? UserId { get; private set; }

    public DateTime AccessedAtUtc { get; private set; }

    public bool WasSuccessful { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public string? FailureReason { get; private set; }

    public ShareLink? ShareLink { get; private set; }

    public User? User { get; private set; }
}
