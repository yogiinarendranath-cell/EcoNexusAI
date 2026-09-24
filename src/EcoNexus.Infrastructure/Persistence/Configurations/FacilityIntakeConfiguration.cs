using EcoNexus.Domain.Entities;
using EcoNexus.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoNexus.Infrastructure.Persistence.Configurations;

public sealed class FacilityIntakeConfiguration : IEntityTypeConfiguration<FacilityIntake>
{
    public void Configure(EntityTypeBuilder<FacilityIntake> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("FacilityIntakes");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.FacilityId)
            .HasColumnName("FacilityId")
            .IsRequired();

        builder.Property(i => i.Material)
            .HasConversion<string>()
            .HasColumnName("Material")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(i => i.Weight)
            .HasConversion(
                weight => weight.Kilograms,
                value => Weight.FromKilograms(value))
            .HasColumnName("WeightKilograms")
            .IsRequired();

        builder.Property(i => i.Stage)
            .HasConversion<string>()
            .HasColumnName("Stage")
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(i => i.Stage)
            .HasDatabaseName("IX_FacilityIntakes_Stage");

        builder.Property(i => i.RecordedAt).HasColumnName("RecordedAt").IsRequired();
        builder.Property(i => i.StageUpdatedAt).HasColumnName("StageUpdatedAt");

        // Fast lookup: "intakes for this facility, newest first"
        builder.HasIndex(i => new { i.FacilityId, i.RecordedAt })
            .HasDatabaseName("IX_FacilityIntakes_FacilityId_RecordedAt");
    }
}
