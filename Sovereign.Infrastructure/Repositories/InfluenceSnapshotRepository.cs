using Microsoft.EntityFrameworkCore;
using Sovereign.Application.Interfaces;
using Sovereign.Domain.Entities;
using Sovereign.Infrastructure.Persistence;

namespace Sovereign.Infrastructure.Repositories;

public sealed class InfluenceSnapshotRepository : IInfluenceSnapshotRepository
{
    private readonly SovereignDbContext _dbContext;

    public InfluenceSnapshotRepository(SovereignDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(InfluenceSnapshot snapshot, CancellationToken ct = default)
        => await _dbContext.Set<InfluenceSnapshot>().AddAsync(snapshot, ct);

    public async Task<IReadOnlyList<InfluenceSnapshot>> GetByUserAsync(string userId, CancellationToken ct = default)
        => await _dbContext.Set<InfluenceSnapshot>()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CapturedAtUtc)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _dbContext.SaveChangesAsync(ct);
}
