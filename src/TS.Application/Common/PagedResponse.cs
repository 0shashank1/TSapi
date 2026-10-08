namespace TS.Application.Common;

public sealed class PagedResponse<T>
{
    public PagedResponse(IReadOnlyList<T> items, string? nextCursor)
    {
        Items = items;
        NextCursor = nextCursor;
    }

    public IReadOnlyList<T> Items { get; }

    public string? NextCursor { get; }
}
