using TS.Domain.Entities;

namespace TS.Application.DTOs.Snippets;

public sealed class SnippetResponse
{
    public SnippetResponse(
        Guid id,
        string? title,
        string content,
        DateTime? expiresAtUtc,
        int? maxViews,
        long viewCount,
        DateTime? lastViewedAtUtc,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        Guid version)
    {
        Id = id;
        Title = title;
        Content = content;
        ExpiresAtUtc = expiresAtUtc;
        MaxViews = maxViews;
        ViewCount = viewCount;
        LastViewedAtUtc = lastViewedAtUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        Version = version;
    }

    public Guid Id { get; }

    public string? Title { get; }

    public string Content { get; }

    public DateTime? ExpiresAtUtc { get; }

    public int? MaxViews { get; }

    public long ViewCount { get; }

    public DateTime? LastViewedAtUtc { get; }

    public DateTime CreatedAtUtc { get; }

    public DateTime UpdatedAtUtc { get; }

    /// <summary>Optimistic concurrency token; send as If-Match on PATCH.</summary>
    public Guid Version { get; }

    public static SnippetResponse From(TextSnippet snippet)
        => new(
            snippet.Id,
            snippet.Title,
            snippet.Content,
            snippet.ExpiresAtUtc,
            snippet.MaxViews,
            snippet.ViewCount,
            snippet.LastViewedAtUtc,
            snippet.CreatedAtUtc,
            snippet.UpdatedAtUtc,
            snippet.Version);
}
