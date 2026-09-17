using EcoNexus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoNexus.Infrastructure.Persistence.Configurations;

public sealed class CollectionJobConfiguration : IEntityTypeConfiguration<CollectionJob>
{
    public void Configure(EntityTypeBuilder<CollectionJob> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("CollectionJobs");
        builder.HasKey(j => j.Id);

        builder.Property(j => j.VehicleId)
            .HasColumnName("VehicleId")
            .IsRequired();

        builder.Property(j => j.Status)
            .HasConversion<string>()
            .HasColumnName("Status")
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(j => j.Status)
            .HasDatabaseName("IX_CollectionJobs_Status");

        builder.Property(j => j.ScheduledFor)
            .HasColumnName("ScheduledFor")
            .IsRequired();

        builder.Property(j => j.StartedAt)
            .HasColumnName("StartedAt");

        builder.Property(j => j.CompletedAt)
            .HasColumnName("CompletedAt");

        builder.Ignore(j => j.TotalCollected);

        // ---- Child collection: stops ----
        builder.HasMany(j => j.Stops)
            .WithOne()
            .HasForeignKey("JobId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(j => j.Stops)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(j => new { j.VehicleId, j.ScheduledFor })
            .HasDatabaseName("IX_CollectionJobs_VehicleId_ScheduledFor");
    }
}
