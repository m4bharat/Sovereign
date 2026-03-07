using Microsoft.EntityFrameworkCore;
using Sovereign.Application.Interfaces;
using Sovereign.Domain.Entities;
using Sovereign.Infrastructure.Persistence;

namespace Sovereign.Infrastructure.Repositories;

public sealed class SocialEdgeRepository : ISocialEdgeRepository
{
    private readonly SovereignDbContext _dbContext;

    public SocialEdgeRepository(SovereignDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SocialEdge?> GetAsync(string sourceUserId, string targetContactId, CancellationToken ct = default)
        => await _dbContext.Set<SocialEdge>()
            .FirstOrDefaultAsync(x => x.SourceUserId == sourceUserId && x.TargetContactId == targetContactId, ct);

    public async Task<IReadOnlyList<SocialEdge>> GetByUserAsync(string userId, CancellationToken ct = default)
        => await _dbContext.Set<SocialEdge>()
            .Where(x => x.SourceUserId == userId)
            .ToListAsync(ct);

    public async Task AddAsync(SocialEdge edge, CancellationToken ct = default)
        => await _dbContext.Set<SocialEdge>().AddAsync(edge, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _dbContext.SaveChangesAsync(ct);
}
