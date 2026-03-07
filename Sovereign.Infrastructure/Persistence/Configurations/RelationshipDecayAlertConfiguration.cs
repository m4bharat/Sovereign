using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sovereign.Domain.Entities;

namespace Sovereign.Infrastructure.Persistence.Configurations;

public sealed class RelationshipDecayAlertConfiguration : IEntityTypeConfiguration<RelationshipDecayAlert>
{
    public void Configure(EntityTypeBuilder<RelationshipDecayAlert> builder)
    {
        builder.ToTable("relationship_decay_alerts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RelationshipId).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.TriggeredAtUtc).IsRequired();
        builder.HasIndex(x => new { x.RelationshipId, x.TriggeredAtUtc });
    }
}
