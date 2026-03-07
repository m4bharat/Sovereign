
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sovereign.Domain.Aggregates;

namespace Sovereign.Infrastructure.Persistence.Configurations;

public class RelationshipConfiguration : IEntityTypeConfiguration<Relationship>
{
    public void Configure(EntityTypeBuilder<Relationship> builder)
    {
        builder.ToTable("relationships");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.PowerDifferential)
            .IsRequired();

        builder.Property(r => r.ReciprocityScore)
            .IsRequired();

        builder.Property(r => r.LastInteractionAtUtc)
            .IsRequired();
    }
}
