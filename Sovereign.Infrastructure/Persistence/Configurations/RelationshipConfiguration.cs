using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sovereign.Domain.Aggregates;

namespace Sovereign.Infrastructure.Persistence.Configurations;

public sealed class RelationshipConfiguration : IEntityTypeConfiguration<Relationship>
{
    public void Configure(EntityTypeBuilder<Relationship> builder)
    {
        builder.ToTable("relationships");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.ContactId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Role)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.ReciprocityScore).IsRequired();
        builder.Property(x => x.MomentumScore).IsRequired();
        builder.Property(x => x.PowerDifferential).IsRequired();
        builder.Property(x => x.EmotionalTemperature).IsRequired();
        builder.Property(x => x.LastInteractionAtUtc).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.ContactId }).IsUnique();
        builder.HasIndex(x => x.LastInteractionAtUtc);
    }
}
