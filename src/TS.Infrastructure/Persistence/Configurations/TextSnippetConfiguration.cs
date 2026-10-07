using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TS.Domain.Entities;

namespace TS.Infrastructure.Persistence.Configurations;

public sealed class TextSnippetConfiguration
    : IEntityTypeConfiguration<TextSnippet>
{
    public void Configure(EntityTypeBuilder<TextSnippet> builder)
    {
        builder.ToTable("text_snippets", table =>
        {
            table.HasCheckConstraint(
                "ck_text_snippets_content_length",
                "octet_length(content) <= 1048576");

            table.HasCheckConstraint(
                "ck_text_snippets_max_views",
                "max_views IS NULL OR max_views > 0");

            table.HasCheckConstraint(
                "ck_text_snippets_view_count",
                "view_count >= 0");

            table.HasCheckConstraint(
                "ck_text_snippets_view_limit",
                "max_views IS NULL OR view_count <= max_views");

            table.HasCheckConstraint(
                "ck_text_snippets_expiration",
                "expires_at_utc IS NULL OR expires_at_utc > created_at_utc");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.OwnerUserId)
            .HasColumnName("owner_user_id")
            .IsRequired();

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(200);

        builder.Property(x => x.Content)
            .HasColumnName("content")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.MaxViews)
            .HasColumnName("max_views");

        builder.Property(x => x.ViewCount)
            .HasColumnName("view_count")
            .HasDefaultValue(0L)
            .IsRequired();

        builder.Property(x => x.LastViewedAtUtc)
            .HasColumnName("last_viewed_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.Version)
            .HasColumnName("version")
            .IsConcurrencyToken()
            .IsRequired();

        // User dashboard:
        // WHERE owner_user_id = ?
        // ORDER BY created_at_utc DESC
        builder.HasIndex(x => new
        {
            x.OwnerUserId,
            x.CreatedAtUtc
        })
        .HasDatabaseName("ix_text_snippets_owner_created");

        // Background cleanup:
        // WHERE expires_at_utc <= ?
        builder.HasIndex(x => x.ExpiresAtUtc)
            .HasDatabaseName("ix_text_snippets_expires_at");

        builder.HasMany(x => x.ShareLinks)
            .WithOne(x => x.TextSnippet)
            .HasForeignKey(x => x.TextSnippetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
