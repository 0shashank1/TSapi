using Microsoft.EntityFrameworkCore;
using TS.Infrastructure.Persistence;

namespace TS.IntegrationTests;

public class QueryTranslationTests
{
    private static TSDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TSDbContext>()
            .UseNpgsql("Host=localhost;Database=ts;Username=postgres;Password=postgres")
            .Options;

        return new TSDbContext(options);
    }

    [Fact]
    public void Guid_and_string_CompareTo_translate()
    {
        using var db = CreateContext();
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var term = "abc";
        var title = "zzz";

        var guidQuery = db.TextSnippets.AsNoTracking()
            .Where(s => s.CreatedAtUtc < now ||
                        (s.CreatedAtUtc == now && s.Id.CompareTo(id) < 0))
            .OrderByDescending(s => s.CreatedAtUtc)
            .ThenByDescending(s => s.Id)
            .Take(21);

        var sql1 = guidQuery.ToQueryString();
        Assert.Contains("created_at_utc", sql1);

        var stringQuery = db.TextSnippets.AsNoTracking()
            .Where(s => (s.Title ?? "").CompareTo(title) > 0 ||
                        ((s.Title ?? "") == title && s.Id.CompareTo(id) > 0))
            .OrderBy(s => s.Title ?? "")
            .ThenBy(s => s.Id);

        var sql2 = stringQuery.ToQueryString();
        Assert.Contains("title", sql2);

        var searchQuery = db.TextSnippets.AsNoTracking()
            .Where(s => s.Title != null && s.Title.Contains(term));

        var sql3 = searchQuery.ToQueryString();
        Assert.Contains("LIKE", sql3);

        var counts = db.Users.AsNoTracking()
            .Select(u => new
            {
                u.Id,
                SnippetCount = u.Snippets.Count,
                ActiveRefreshTokenCount = u.RefreshTokens.Count(
                    t => t.RevokedAtUtc == null && t.ExpiresAtUtc > now)
            });

        var sql4 = counts.ToQueryString();
        Assert.NotNull(sql4);
    }
}
