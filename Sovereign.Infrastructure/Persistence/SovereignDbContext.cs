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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SovereignDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
