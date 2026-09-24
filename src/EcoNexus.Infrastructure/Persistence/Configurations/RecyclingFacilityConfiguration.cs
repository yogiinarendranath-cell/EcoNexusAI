using EcoNexus.Domain.Entities;
using EcoNexus.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoNexus.Infrastructure.Persistence.Configurations;

public sealed class RecyclingFacilityConfiguration : IEntityTypeConfiguration<RecyclingFacility>
{
    public void Configure(EntityTypeBuilder<RecyclingFacility> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RecyclingFacilities");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .HasColumnName("Name")
            .HasMaxLength(120)
            .IsRequired();

        builder.OwnsOne(f => f.Location, location =>
        {
            location.Property(l => l.Latitude).HasColumnName("Latitude").IsRequired();
            location.Property(l => l.Longitude).HasColumnName("Longitude").IsRequired();
        });

        builder.Navigation(f => f.Location).IsRequired();

        builder.Property(f => f.DailyCapacity)
            .HasConversion(
                weight => weight.Kilograms,
                value => Weight.FromKilograms(value))
            .HasColumnName("DailyCapacityKilograms")
            .IsRequired();

        builder.Property(f => f.Status)
            .HasConversion<string>()
            .HasColumnName("Status")
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(f => f.Status)
            .HasDatabaseName("IX_RecyclingFacilities_Status");

        builder.Property(f => f.CreatedAt).HasColumnName("CreatedAt").IsRequired();
        builder.Property(f => f.LastUpdatedAt).HasColumnName("LastUpdatedAt").IsRequired();

        // Child collection: intakes. EF discovers private backing field '_intakes'.
        builder.HasMany(f => f.Intakes)
            .WithOne()
            .HasForeignKey("FacilityId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(f => f.Intakes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
