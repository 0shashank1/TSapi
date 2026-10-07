using TS.Domain.Common;

namespace TS.Domain.Entities;

public sealed class TextSnippet : EntityBase
{
    private TextSnippet()
    {
        // EF Core constructor
    }

    public TextSnippet(
        Guid ownerUserId,
        string content,
        string? title,
        DateTime? expiresAtUtc,
        int? maxViews)
    {
        OwnerUserId = ownerUserId;
        Content = content;
        Title = title;
        ExpiresAtUtc = expiresAtUtc;
        MaxViews = maxViews;
        ViewCount = 0;
    }

    public Guid OwnerUserId { get; private set; }

    public string? Title { get; private set; }

    public string Content { get; private set; } = null!;

    public DateTime? ExpiresAtUtc { get; private set; }

    public int? MaxViews { get; private set; }

    public long ViewCount { get; private set; }

    public DateTime? LastViewedAtUtc { get; private set; }

    public User Owner { get; private set; } = null!;

    public ICollection<ShareLink> ShareLinks { get; private set; }
        = new List<ShareLink>();

    public bool IsExpired(DateTime utcNow)
    {
        return ExpiresAtUtc.HasValue &&
               ExpiresAtUtc.Value <= utcNow;
    }

    public bool HasReachedViewLimit()
    {
        return MaxViews.HasValue &&
               ViewCount >= MaxViews.Value;
    }

    public void RecordView(DateTime utcNow)
    {
        ViewCount++;
        LastViewedAtUtc = utcNow;

        Touch(utcNow);
    }

    public void UpdateContent(
        string content,
        string? title,
        DateTime? expiresAtUtc,
        int? maxViews,
        DateTime utcNow)
    {
        Content = content;
        Title = title;
        ExpiresAtUtc = expiresAtUtc;
        MaxViews = maxViews;

        Touch(utcNow);
    }
}
