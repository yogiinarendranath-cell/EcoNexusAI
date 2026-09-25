using EcoNexus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoNexus.Infrastructure.Persistence.Configurations;

public sealed class CitizenProfileConfiguration : IEntityTypeConfiguration<CitizenProfile>
{
    public void Configure(EntityTypeBuilder<CitizenProfile> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("CitizenProfiles");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId)
            .HasColumnName("UserId")
            .IsRequired();

        builder.HasIndex(c => c.UserId)
            .IsUnique()
            .HasDatabaseName("UX_CitizenProfiles_UserId");

        builder.Property(c => c.DisplayName)
            .HasColumnName("DisplayName")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(c => c.HomeAddress)
            .HasColumnName("HomeAddress")
            .HasMaxLength(300);

        builder.Property(c => c.HomeLatitude).HasColumnName("HomeLatitude");
        builder.Property(c => c.HomeLongitude).HasColumnName("HomeLongitude");

        builder.Property(c => c.CurrentStreakDays)
            .HasColumnName("CurrentStreakDays")
            .IsRequired();

        builder.Property(c => c.LastVisitDate).HasColumnName("LastVisitDate");

        builder.Property(c => c.CreatedAt).HasColumnName("CreatedAt").IsRequired();
        builder.Property(c => c.LastUpdatedAt).HasColumnName("LastUpdatedAt").IsRequired();

        // GreenPointsBalance is a derived property (sum of ledger) and must
        // NOT be persisted — otherwise EF will try to map it and the model
        // will diverge from the intended design.
        builder.Ignore(c => c.GreenPointsBalance);

        // Child collection: green point transactions.
        builder.HasMany(c => c.Transactions)
            .WithOne()
            .HasForeignKey("CitizenProfileId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.Transactions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
