using EcoNexus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoNexus.Infrastructure.Persistence.Configurations;

public sealed class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Alerts");
        builder.HasKey(a => a.Id);

        // Nullable FK: some alerts are system-wide
        builder.Property(a => a.StationId)
            .HasColumnName("StationId");

        builder.HasIndex(a => a.StationId)
            .HasDatabaseName("IX_Alerts_StationId");

        // AlertSeverity as string
        builder.Property(a => a.Severity)
            .HasConversion<string>()
            .HasColumnName("Severity")
            .HasMaxLength(20)
            .IsRequired();

        // ReportStatus as string (reused enum for lifecycle)
        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasColumnName("Status")
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(a => a.Status)
            .HasDatabaseName("IX_Alerts_Status");

        builder.Property(a => a.Message)
            .HasColumnName("Message")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(a => a.RaisedAt)
            .HasColumnName("RaisedAt")
            .IsRequired();

        builder.Property(a => a.AcknowledgedAt)
            .HasColumnName("AcknowledgedAt");

        builder.Property(a => a.ResolvedAt)
            .HasColumnName("ResolvedAt");

        builder.Property(a => a.AcknowledgedBy)
            .HasColumnName("AcknowledgedBy")
            .HasMaxLength(100);

        builder.Property(a => a.ResolvedBy)
            .HasColumnName("ResolvedBy")
            .HasMaxLength(100);
    }
}
