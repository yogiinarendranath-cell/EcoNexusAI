using EcoNexus.Domain.Entities;
using EcoNexus.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoNexus.Infrastructure.Persistence.Configurations;

public sealed class WasteStationConfiguration : IEntityTypeConfiguration<WasteStation>
{
    public void Configure(EntityTypeBuilder<WasteStation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("WasteStations");
        builder.HasKey(s => s.Id);

        // ---- Value objects ----

        builder.Property(s => s.Code)
            .HasConversion(
                code => code.Value,
                value => StationCode.Create(value))
            .HasColumnName("Code")
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(s => s.Code)
            .IsUnique()
            .HasDatabaseName("UX_WasteStations_Code");

        builder.OwnsOne(s => s.Location, location =>
        {
            location.Property(l => l.Latitude).HasColumnName("Latitude").IsRequired();
            location.Property(l => l.Longitude).HasColumnName("Longitude").IsRequired();
        });

        builder.Navigation(s => s.Location).IsRequired();

        builder.Property(s => s.Capacity)
            .HasConversion(
                weight => weight.Kilograms,
                value => Weight.FromKilograms(value))
            .HasColumnName("CapacityKilograms")
            .IsRequired();

        builder.Property(s => s.CurrentFill)
            .HasConversion(
                fill => fill.Percent,
                value => FillLevel.FromPercent(value))
            .HasColumnName("CurrentFillPercent")
            .IsRequired();

        builder.Property(s => s.PrimaryCategory)
            .HasConversion<string>()
            .HasColumnName("PrimaryCategory")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasColumnName("Status")
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(s => s.Status)
            .HasDatabaseName("IX_WasteStations_Status");

        builder.Property(s => s.LastUpdatedAt).HasColumnName("LastUpdatedAt").IsRequired();
        builder.Property(s => s.LastCollectedAt).HasColumnName("LastCollectedAt");

        // ---- Child collection: readings ----
        // Configure using the public navigation; EF Core discovers the
        // private backing field '_readings' automatically.
        builder.HasMany(s => s.Readings)
            .WithOne()
            .HasForeignKey("StationId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Readings)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
