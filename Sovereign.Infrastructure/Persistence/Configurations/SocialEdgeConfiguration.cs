using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sovereign.Domain.Entities;

namespace Sovereign.Infrastructure.Persistence.Configurations;

public sealed class SocialEdgeConfiguration : IEntityTypeConfiguration<SocialEdge>
{
    public void Configure(EntityTypeBuilder<SocialEdge> builder)
    {
        builder.ToTable("social_edges");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SourceUserId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.TargetContactId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.StrengthScore).IsRequired();
        builder.Property(x => x.InfluenceScore).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.HasIndex(x => new { x.SourceUserId, x.TargetContactId }).IsUnique();
    }
}
