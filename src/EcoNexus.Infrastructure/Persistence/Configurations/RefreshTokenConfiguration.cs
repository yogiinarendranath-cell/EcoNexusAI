using EcoNexus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcoNexus.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RefreshTokens");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId)
            .HasColumnName("UserId")
            .IsRequired();

        builder.HasIndex(t => t.UserId)
            .HasDatabaseName("IX_RefreshTokens_UserId");

        builder.Property(t => t.TokenHash)
            .HasColumnName("TokenHash")
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(t => t.TokenHash)
            .IsUnique()
            .HasDatabaseName("UX_RefreshTokens_TokenHash");

        builder.Property(t => t.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(t => t.ExpiresAt)
            .HasColumnName("ExpiresAt")
            .IsRequired();

        builder.Property(t => t.RevokedAt)
            .HasColumnName("RevokedAt");

        builder.Property(t => t.ReplacedByTokenHash)
            .HasColumnName("ReplacedByTokenHash")
            .HasMaxLength(128);
    }
}
