using EcoNexus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoNexus.Infrastructure.Persistence.Configurations;

public sealed class AssistantInteractionConfiguration
    : IEntityTypeConfiguration<AssistantInteraction>
{
    public void Configure(EntityTypeBuilder<AssistantInteraction> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("AssistantInteractions");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId)
            .HasColumnName("UserId")
            .IsRequired();

        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("IX_AssistantInteractions_UserId");

        builder.Property(a => a.Question)
            .HasColumnName("Question")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(a => a.ToolName)
            .HasColumnName("ToolName")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.ToolParametersJson)
            .HasColumnName("ToolParametersJson")
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(a => a.ToolResultJson)
            .HasColumnName("ToolResultJson")
            .HasMaxLength(8000)
            .IsRequired();

        builder.Property(a => a.Answer)
            .HasColumnName("Answer")
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(a => a.ProviderName)
            .HasColumnName("ProviderName")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.LatencyMs)
            .HasColumnName("LatencyMs")
            .IsRequired();

        builder.Property(a => a.OccurredAt)
            .HasColumnName("OccurredAt")
            .IsRequired();

        // Fast lookup: recent interactions for a user, newest first.
        builder.HasIndex(a => new { a.UserId, a.OccurredAt })
            .HasDatabaseName("IX_AssistantInteractions_UserId_OccurredAt");
    }
}
