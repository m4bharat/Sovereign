using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sovereign.Domain.Entities;

namespace Sovereign.Infrastructure.Persistence.Configurations;

public sealed class SuggestionEventConfiguration : IEntityTypeConfiguration<SuggestionEvent>
{
    public void Configure(EntityTypeBuilder<SuggestionEvent> builder)
    {
        builder.ToTable("suggestion_events");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.TenantId).HasMaxLength(128);
        builder.Property(x => x.SessionId).HasMaxLength(128);
        builder.Property(x => x.EventType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.EventTime).IsRequired();
        builder.Property(x => x.Platform).HasMaxLength(64);
        builder.Property(x => x.Surface).HasMaxLength(128);
        builder.Property(x => x.CurrentUrl).HasMaxLength(2048);
        builder.Property(x => x.SituationType).HasMaxLength(128);
        builder.Property(x => x.Move).HasMaxLength(128);
        builder.Property(x => x.Strategy).HasMaxLength(128);
        builder.Property(x => x.Tone).HasMaxLength(64);
        builder.Property(x => x.SourceAuthor).HasMaxLength(256);
        builder.Property(x => x.SourceTitle).HasMaxLength(512);
        builder.Property(x => x.SourceTextHash).HasMaxLength(128);
        builder.Property(x => x.InputMessageHash).HasMaxLength(128);
        builder.Property(x => x.ReplyHash).HasMaxLength(128);
        builder.Property(x => x.ModelProvider).HasMaxLength(128);
        builder.Property(x => x.ModelName).HasMaxLength(128);
        builder.Property(x => x.MetadataJson).HasColumnType("jsonb");

        builder.HasIndex(x => new { x.UserId, x.EventTime });
        builder.HasIndex(x => new { x.Surface, x.EventTime });
        builder.HasIndex(x => x.SuggestionId);
        builder.HasIndex(x => x.EventType);
    }
}
