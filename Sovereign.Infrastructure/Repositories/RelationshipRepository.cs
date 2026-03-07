using Microsoft.EntityFrameworkCore;
using Sovereign.Application.Interfaces;
using Sovereign.Domain.Aggregates;
using Sovereign.Infrastructure.Persistence;

namespace Sovereign.Infrastructure.Repositories;

public sealed class RelationshipRepository : IRelationshipRepository
{
    private readonly SovereignDbContext _dbContext;

    public RelationshipRepository(SovereignDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Relationship?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _dbContext.Relationships.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Relationship>> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => await _dbContext.Relationships
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.LastInteractionAtUtc)
            .ToListAsync(ct);

    public async Task AddAsync(Relationship relationship, CancellationToken ct = default)
        => await _dbContext.Relationships.AddAsync(relationship, ct);

    public Task UpdateAsync(Relationship relationship, CancellationToken ct = default)
    {
        _dbContext.Relationships.Update(relationship);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _dbContext.SaveChangesAsync(ct);
}
