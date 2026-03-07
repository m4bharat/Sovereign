using Microsoft.EntityFrameworkCore;
using Sovereign.Domain.Aggregates;

namespace Sovereign.Infrastructure.Persistence;

public class SovereignDbContext : DbContext
{
    public SovereignDbContext(DbContextOptions<SovereignDbContext> options)
        : base(options)
    {
    }

    public DbSet<Relationship> Relationships => Set<Relationship>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SovereignDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
