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

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _dbContext.SaveChangesAsync(ct);
}
