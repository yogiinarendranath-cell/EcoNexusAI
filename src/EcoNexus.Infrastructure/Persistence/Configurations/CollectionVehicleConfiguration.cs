using EcoNexus.Domain.Entities;
using EcoNexus.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoNexus.Infrastructure.Persistence.Configurations;

public sealed class CollectionVehicleConfiguration : IEntityTypeConfiguration<CollectionVehicle>
{
    public void Configure(EntityTypeBuilder<CollectionVehicle> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("CollectionVehicles");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.RegistrationNumber)
            .HasColumnName("RegistrationNumber")
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(v => v.RegistrationNumber)
            .IsUnique()
            .HasDatabaseName("UX_CollectionVehicles_RegistrationNumber");

        builder.Property(v => v.Capacity)
            .HasConversion(
                weight => weight.Kilograms,
                value => Weight.FromKilograms(value))
            .HasColumnName("CapacityKilograms")
            .IsRequired();

        builder.Property(v => v.IsActive)
            .HasColumnName("IsActive")
            .IsRequired();
    }
}
