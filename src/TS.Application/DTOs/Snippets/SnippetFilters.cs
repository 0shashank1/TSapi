namespace TS.Application.DTOs.Snippets;

/// <summary>all | active | expired</summary>
public enum SnippetStatus
{
    All,
    Active,
    Expired
}

/// <summary>createdAt | title | viewCount | updatedAt</summary>
public enum SnippetSortBy
{
    CreatedAt,
    Title,
    ViewCount,
    UpdatedAt
}
