using Microsoft.EntityFrameworkCore;
using Sovereign.Domain.Aggregates;
using Sovereign.Domain.Entities;

namespace Sovereign.Infrastructure.Persistence;

public class SovereignDbContext : DbContext
{
    public SovereignDbContext(DbContextOptions<SovereignDbContext> options)
        : base(options)
    {
    }

    public DbSet<Relationship> Relationships => Set<Relationship>();
    public DbSet<MemoryEntry> Memories => Set<MemoryEntry>();
    public DbSet<ConversationThread> ConversationThreads => Set<ConversationThread>();
    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();
    public DbSet<ConversationSummary> ConversationSummaries => Set<ConversationSummary>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<RelationshipDecayAlert> RelationshipDecayAlerts => Set<RelationshipDecayAlert>();
    public DbSet<SocialEdge> SocialEdges => Set<SocialEdge>();
    public DbSet<InfluenceSnapshot> InfluenceSnapshots => Set<InfluenceSnapshot>();
    public DbSet<SuggestionEvent> SuggestionEvents => Set<SuggestionEvent>();
    public DbSet<SuggestionFeedback> SuggestionFeedback => Set<SuggestionFeedback>();
    public DbSet<SuggestionSnapshot> SuggestionSnapshots => Set<SuggestionSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SovereignDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
