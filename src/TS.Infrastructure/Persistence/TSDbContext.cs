using Microsoft.EntityFrameworkCore;
using TS.Domain.Entities;

namespace TS.Infrastructure.Persistence;

public sealed class TSDbContext (DbContextOptions<TSDbContext> options): DbContext(options)
{

    public DbSet<User> Users => Set<User>();

    public DbSet<TextSnippet> TextSnippets => Set<TextSnippet>();

    public DbSet<ShareLink> ShareLinks => Set<ShareLink>();

    public DbSet<ShareAccessLog> ShareAccessLogs => Set<ShareAccessLog>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TSDbContext).Assembly);
    }
}
