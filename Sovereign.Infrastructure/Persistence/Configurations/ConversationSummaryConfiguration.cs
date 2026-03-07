using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sovereign.Domain.Entities;

namespace Sovereign.Infrastructure.Persistence.Configurations;

public sealed class ConversationSummaryConfiguration : IEntityTypeConfiguration<ConversationSummary>
{
    public void Configure(EntityTypeBuilder<ConversationSummary> builder)
    {
        builder.ToTable("conversation_summaries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ThreadId).IsRequired();
        builder.Property(x => x.SummaryText).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.GeneratedAtUtc).IsRequired();
        builder.HasIndex(x => new { x.ThreadId, x.GeneratedAtUtc });
    }
}
