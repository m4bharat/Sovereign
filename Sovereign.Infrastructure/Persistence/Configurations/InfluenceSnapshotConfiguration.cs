using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sovereign.Domain.Entities;

namespace Sovereign.Infrastructure.Persistence.Configurations;

public sealed class InfluenceSnapshotConfiguration : IEntityTypeConfiguration<InfluenceSnapshot>
{
    public void Configure(EntityTypeBuilder<InfluenceSnapshot> builder)
    {
        builder.ToTable("influence_snapshots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.AggregateInfluenceScore).IsRequired();
        builder.Property(x => x.CapturedAtUtc).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.CapturedAtUtc });
    }
}
