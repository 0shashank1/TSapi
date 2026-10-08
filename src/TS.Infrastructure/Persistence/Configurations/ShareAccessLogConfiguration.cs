using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TS.Domain.Entities;

namespace TS.Infrastructure.Persistence.Configurations;

public sealed class ShareAccessLogConfiguration
    : IEntityTypeConfiguration<ShareAccessLog>
{
    public void Configure(EntityTypeBuilder<ShareAccessLog> builder)
    {
        builder.ToTable("share_access_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.ShareLinkId)
            .HasColumnName("share_link_id");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id");

        builder.Property(x => x.AccessedAtUtc)
            .HasColumnName("accessed_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.WasSuccessful)
            .HasColumnName("was_successful")
            .IsRequired();

        builder.Property(x => x.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45);

        builder.Property(x => x.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(1000);

        builder.Property(x => x.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(100);

        builder.HasIndex(x => new
        {
            x.ShareLinkId,
            x.AccessedAtUtc
        })
        .HasDatabaseName("ix_share_access_logs_link_time");

        builder.HasIndex(x => x.AccessedAtUtc)
            .HasDatabaseName("ix_share_access_logs_accessed_at");

        builder.HasOne(x => x.ShareLink)
            .WithMany(x => x.AccessLogs)
            .HasForeignKey(x => x.ShareLinkId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.User)
            .WithMany(x => x.AccessLogs)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
