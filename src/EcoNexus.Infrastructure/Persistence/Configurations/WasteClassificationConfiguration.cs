using EcoNexus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoNexus.Infrastructure.Persistence.Configurations;

public sealed class WasteClassificationConfiguration : IEntityTypeConfiguration<WasteClassification>
{
    public void Configure(EntityTypeBuilder<WasteClassification> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("WasteClassifications");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CitizenProfileId)
            .HasColumnName("CitizenProfileId")
            .IsRequired();

        builder.Property(c => c.Category)
            .HasConversion<string>()
            .HasColumnName("Category")
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(c => c.Category)
            .HasDatabaseName("IX_WasteClassifications_Category");

        builder.Property(c => c.Confidence)
            .HasColumnName("Confidence")
            .IsRequired();

        builder.Property(c => c.IsRecyclable)
            .HasColumnName("IsRecyclable")
            .IsRequired();

        builder.Property(c => c.IsCompostable)
            .HasColumnName("IsCompostable")
            .IsRequired();

        builder.Property(c => c.DisposalInstruction)
            .HasColumnName("DisposalInstruction")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(c => c.ImageReference)
            .HasColumnName("ImageReference")
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(c => c.ProviderName)
            .HasColumnName("ProviderName")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.ClassifiedAt)
            .HasColumnName("ClassifiedAt")
            .IsRequired();

        // Fast lookup: "recent classifications for this citizen"
        builder.HasIndex(c => new { c.CitizenProfileId, c.ClassifiedAt })
            .HasDatabaseName("IX_WasteClassifications_CitizenProfileId_ClassifiedAt");

        // Audit: "how often is the Ollama provider returning low-confidence results?"
        builder.HasIndex(c => new { c.ProviderName, c.Confidence })
            .HasDatabaseName("IX_WasteClassifications_ProviderName_Confidence");
    }
}
