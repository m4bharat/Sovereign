using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sovereign.Domain.Entities;

namespace Sovereign.Infrastructure.Persistence.Configurations;

public sealed class SuggestionFeedbackConfiguration : IEntityTypeConfiguration<SuggestionFeedback>
{
    public void Configure(EntityTypeBuilder<SuggestionFeedback> builder)
    {
        builder.ToTable("suggestion_feedback");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.SuggestionId).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.FeedbackType).HasMaxLength(128);
        builder.Property(x => x.FeedbackText).HasMaxLength(4000);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.SuggestionId);
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
    }
}
