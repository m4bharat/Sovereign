using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sovereign.Domain.Entities;

namespace Sovereign.Infrastructure.Persistence.Configurations;

public sealed class SuggestionSnapshotConfiguration : IEntityTypeConfiguration<SuggestionSnapshot>
{
    public void Configure(EntityTypeBuilder<SuggestionSnapshot> builder)
    {
        builder.ToTable("suggestion_snapshots");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.SuggestionId).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RequestPayloadJson).HasColumnType("jsonb");
        builder.Property(x => x.ResponsePayloadJson).HasColumnType("jsonb");
        builder.Property(x => x.SourceText).HasMaxLength(12000);
        builder.Property(x => x.InputMessage).HasMaxLength(12000);
        builder.Property(x => x.GeneratedReply).HasMaxLength(12000);
        builder.Property(x => x.EditedReply).HasMaxLength(12000);
        builder.Property(x => x.IsDebugSample).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.SuggestionId).IsUnique();
    }
}
