using TS.Domain.Entities;

namespace TS.Application.DTOs.Snippets;

public sealed class SnippetListItem
{
    public SnippetListItem(
        Guid id,
        string? title,
        DateTime? expiresAtUtc,
        int? maxViews,
        long viewCount,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        Id = id;
        Title = title;
        ExpiresAtUtc = expiresAtUtc;
        MaxViews = maxViews;
        ViewCount = viewCount;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid Id { get; }

    public string? Title { get; }

    public DateTime? ExpiresAtUtc { get; }

    public int? MaxViews { get; }

    public long ViewCount { get; }

    public DateTime CreatedAtUtc { get; }

    public DateTime UpdatedAtUtc { get; }

    public static SnippetListItem From(TextSnippet snippet)
        => new(
            snippet.Id,
            snippet.Title,
            snippet.ExpiresAtUtc,
            snippet.MaxViews,
            snippet.ViewCount,
            snippet.CreatedAtUtc,
            snippet.UpdatedAtUtc);
}
