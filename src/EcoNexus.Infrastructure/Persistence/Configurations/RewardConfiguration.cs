using EcoNexus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoNexus.Infrastructure.Persistence.Configurations;

public sealed class RewardConfiguration : IEntityTypeConfiguration<Reward>
{
    public void Configure(EntityTypeBuilder<Reward> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Rewards");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .HasColumnName("Name")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasColumnName("Description")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(r => r.CostInPoints)
            .HasColumnName("CostInPoints")
            .IsRequired();

        builder.Property(r => r.IsActive)
            .HasColumnName("IsActive")
            .IsRequired();

        builder.HasIndex(r => r.IsActive)
            .HasDatabaseName("IX_Rewards_IsActive");

        builder.Property(r => r.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();
    }
}
