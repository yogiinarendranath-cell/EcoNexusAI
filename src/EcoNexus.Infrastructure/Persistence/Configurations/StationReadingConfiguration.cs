using EcoNexus.Domain.Entities;
using EcoNexus.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoNexus.Infrastructure.Persistence.Configurations;

public sealed class StationReadingConfiguration : IEntityTypeConfiguration<StationReading>
{
    public void Configure(EntityTypeBuilder<StationReading> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("StationReadings");
        builder.HasKey(r => r.Id);

        // FK back to the aggregate root
        builder.Property(r => r.StationId)
            .HasColumnName("StationId")
            .IsRequired();

        // FillLevel -> double (percent)
        builder.Property(r => r.FillLevel)
            .HasConversion(
                fill => fill.Percent,
                value => FillLevel.FromPercent(value))
            .HasColumnName("FillLevelPercent")
            .IsRequired();

        builder.Property(r => r.TemperatureCelsius)
            .HasColumnName("TemperatureCelsius")
            .IsRequired();

        builder.Property(r => r.BatteryPercent)
            .HasColumnName("BatteryPercent")
            .IsRequired();

        builder.Property(r => r.RecordedAt)
            .HasColumnName("RecordedAt")
            .IsRequired();

        // Index for the most common query: "recent readings for a station"
        builder.HasIndex(r => new { r.StationId, r.RecordedAt })
            .HasDatabaseName("IX_StationReadings_StationId_RecordedAt");
    }
}
