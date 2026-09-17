using EcoNexus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoNexus.Infrastructure.Persistence.Configurations;

public sealed class CitizenReportConfiguration : IEntityTypeConfiguration<CitizenReport>
{
    public void Configure(EntityTypeBuilder<CitizenReport> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("CitizenReports");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.FiledByUserId)
            .HasColumnName("FiledByUserId")
            .IsRequired();

        builder.Property(r => r.StationId)
            .HasColumnName("StationId")
            .IsRequired();

        builder.HasIndex(r => r.StationId)
            .HasDatabaseName("IX_CitizenReports_StationId");

        builder.HasIndex(r => r.FiledByUserId)
            .HasDatabaseName("IX_CitizenReports_FiledByUserId");

        builder.Property(r => r.Description)
            .HasColumnName("Description")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(r => r.PhotoUrl)
            .HasColumnName("PhotoUrl")
            .HasMaxLength(2048);

        // ReportStatus as string
        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasColumnName("Status")
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(r => r.Status)
            .HasDatabaseName("IX_CitizenReports_Status");

        builder.Property(r => r.FiledAt)
            .HasColumnName("FiledAt")
            .IsRequired();

        builder.Property(r => r.AcknowledgedAt)
            .HasColumnName("AcknowledgedAt");

        builder.Property(r => r.ResolvedAt)
            .HasColumnName("ResolvedAt");

        builder.Property(r => r.ResolutionNote)
            .HasColumnName("ResolutionNote")
            .HasMaxLength(2000);
    }
}
