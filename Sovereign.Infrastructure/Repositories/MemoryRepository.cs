using Microsoft.EntityFrameworkCore;
using Sovereign.Application.Interfaces;
using Sovereign.Domain.Entities;
using Sovereign.Infrastructure.Persistence;

namespace Sovereign.Infrastructure.Repositories;

public sealed class MemoryRepository : IMemoryRepository
{
    private readonly SovereignDbContext _dbContext;

    public MemoryRepository(SovereignDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(MemoryEntry memory, CancellationToken ct = default)
        => await _dbContext.Memories.AddAsync(memory, ct);

    public async Task<IReadOnlyList<MemoryEntry>> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => await _dbContext.Memories.Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct);

    public async Task<MemoryEntry?> FindExactAsync(string userId, string key, string value, CancellationToken ct = default)
        => await _dbContext.Memories.FirstOrDefaultAsync(x => x.UserId == userId && x.Key == key && x.Value == value, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _dbContext.SaveChangesAsync(ct);
}
