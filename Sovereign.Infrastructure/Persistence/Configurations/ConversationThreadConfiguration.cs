using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sovereign.Domain.Entities;

namespace Sovereign.Infrastructure.Persistence.Configurations;

public sealed class ConversationThreadConfiguration : IEntityTypeConfiguration<ConversationThread>
{
    public void Configure(EntityTypeBuilder<ConversationThread> builder)
    {
        builder.ToTable("conversation_threads");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ContactId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.ContactId }).IsUnique();
    }
}
