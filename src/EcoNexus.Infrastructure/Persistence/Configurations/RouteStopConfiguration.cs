using EcoNexus.Domain.Entities;
using EcoNexus.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoNexus.Infrastructure.Persistence.Configurations;

public sealed class RouteStopConfiguration : IEntityTypeConfiguration<RouteStop>
{
    public void Configure(EntityTypeBuilder<RouteStop> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RouteStops");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.JobId)
            .HasColumnName("JobId")
            .IsRequired();

        builder.Property(s => s.StationId)
            .HasColumnName("StationId")
            .IsRequired();

        builder.Property(s => s.Sequence)
            .HasColumnName("Sequence")
            .IsRequired();

        // CollectedWeight -> nullable double (kilograms)
        builder.Property(s => s.CollectedWeight)
            .HasConversion(
                weight => weight == null ? (double?)null : weight.Kilograms,
                value => value == null ? null : Weight.FromKilograms(value.Value))
            .HasColumnName("CollectedWeightKilograms");

        builder.Property(s => s.CompletedAt)
            .HasColumnName("CompletedAt");

        // Fast lookup: "stops for this job, in order"
        builder.HasIndex(s => new { s.JobId, s.Sequence })
            .HasDatabaseName("IX_RouteStops_JobId_Sequence");
    }
}
