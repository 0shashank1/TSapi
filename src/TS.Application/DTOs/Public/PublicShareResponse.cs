namespace TS.Application.DTOs.Public;

public sealed class PublicShareResponse
{
    public PublicShareResponse(
        Guid id,
        string? title,
        string content,
        DateTime? expiresAtUtc)
    {
        Id = id;
        Title = title;
        Content = content;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid Id { get; }

    public string? Title { get; }

    public string Content { get; }

    public DateTime? ExpiresAtUtc { get; }
}
