
using Microsoft.EntityFrameworkCore;
using Sovereign.Application.Interfaces;
using Sovereign.Domain.Aggregates;
using Sovereign.Infrastructure.Persistence;

namespace Sovereign.Infrastructure.Repositories;

public class RelationshipRepository : IRelationshipRepository
{
    private readonly SovereignDbContext _db;

    public RelationshipRepository(SovereignDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Relationship relationship, CancellationToken ct = default)
    {
        await _db.Relationships.AddAsync(relationship, ct);
    }

    public async Task<Relationship?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Relationships
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public Task UpdateAsync(Relationship relationship, CancellationToken ct = default)
    {
        _db.Relationships.Update(relationship);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}
