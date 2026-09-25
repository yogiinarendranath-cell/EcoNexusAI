using EcoNexus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoNexus.Infrastructure.Persistence.Configurations;

public sealed class GreenPointTransactionConfiguration : IEntityTypeConfiguration<GreenPointTransaction>
{
    public void Configure(EntityTypeBuilder<GreenPointTransaction> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("GreenPointTransactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.CitizenProfileId)
            .HasColumnName("CitizenProfileId")
            .IsRequired();

        builder.Property(t => t.SignedDelta)
            .HasColumnName("SignedDelta")
            .IsRequired();

        builder.Property(t => t.Source)
            .HasConversion<string>()
            .HasColumnName("Source")
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(t => t.Source)
            .HasDatabaseName("IX_GreenPointTransactions_Source");

        builder.Property(t => t.Reason)
            .HasConversion<string>()
            .HasColumnName("Reason")
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(t => t.Reason)
            .HasDatabaseName("IX_GreenPointTransactions_Reason");

        builder.Property(t => t.Description)
            .HasColumnName("Description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(t => t.OccurredAt)
            .HasColumnName("OccurredAt")
            .IsRequired();

        builder.Property(t => t.RelatedEntityId).HasColumnName("RelatedEntityId");

        // Fast lookup: "recent transactions for this citizen"
        builder.HasIndex(t => new { t.CitizenProfileId, t.OccurredAt })
            .HasDatabaseName("IX_GreenPointTransactions_CitizenProfileId_OccurredAt");

        // Rate limit query support: "did this citizen visit this station today?"
        builder.HasIndex(t => new { t.CitizenProfileId, t.Reason, t.OccurredAt })
            .HasDatabaseName("IX_GreenPointTransactions_CitizenProfileId_Reason_OccurredAt");
    }
}
