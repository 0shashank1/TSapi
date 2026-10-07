using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TS.Domain.Entities;

namespace TS.Infrastructure.Persistence.Configurations;

public sealed class ShareLinkConfiguration
    : IEntityTypeConfiguration<ShareLink>
{
    public void Configure(EntityTypeBuilder<ShareLink> builder)
    {
        builder.ToTable("share_links", table =>
        {
            table.HasCheckConstraint(
                "ck_share_links_token_hash_length",
                "octet_length(token_hash) = 32");

            table.HasCheckConstraint(
                "ck_share_links_key_version",
                "token_key_version > 0");

            table.HasCheckConstraint(
                "ck_share_links_max_uses",
                "max_uses IS NULL OR max_uses > 0");

            table.HasCheckConstraint(
                "ck_share_links_use_count",
                "use_count >= 0");

            table.HasCheckConstraint(
                "ck_share_links_use_limit",
                "max_uses IS NULL OR use_count <= max_uses");

            table.HasCheckConstraint(
                "ck_share_links_expiration",
                "expires_at_utc IS NULL OR expires_at_utc > created_at_utc");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.TextSnippetId)
            .HasColumnName("text_snippet_id")
            .IsRequired();

        builder.Property(x => x.TokenHash)
            .HasColumnName("token_hash")
            .HasColumnType("bytea")
            .IsRequired();

        builder.Property(x => x.TokenKeyVersion)
            .HasColumnName("token_key_version")
            .IsRequired();

        builder.Property(x => x.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(255);

        builder.Property(x => x.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.MaxUses)
            .HasColumnName("max_uses");

        builder.Property(x => x.UseCount)
            .HasColumnName("use_count")
            .HasDefaultValue(0L)
            .IsRequired();

        builder.Property(x => x.RevokedAtUtc)
            .HasColumnName("revoked_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.LastAccessedAtUtc)
            .HasColumnName("last_accessed_at_utc")
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

        // Lookup:
        // WHERE token_key_version = ?
        // AND token_hash = ?
        builder.HasIndex(x => new
        {
            x.TokenKeyVersion,
            x.TokenHash
        })
        .IsUnique()
        .HasDatabaseName("ux_share_links_token");

        // Cleanup:
        // WHERE expires_at_utc <= ?
        builder.HasIndex(x => x.ExpiresAtUtc)
            .HasDatabaseName("ix_share_links_expires_at");

        // Get all links for a snippet.
        builder.HasIndex(x => x.TextSnippetId)
            .HasDatabaseName("ix_share_links_text_snippet_id");

        builder.HasOne(x => x.TextSnippet)
            .WithMany(x => x.ShareLinks)
            .HasForeignKey(x => x.TextSnippetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
