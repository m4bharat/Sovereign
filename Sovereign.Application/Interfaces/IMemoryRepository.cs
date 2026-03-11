using Sovereign.Domain.Entities;

namespace Sovereign.Application.Interfaces;

public interface IMemoryRepository
{
    Task AddAsync(MemoryEntry memory, CancellationToken ct = default);
    Task<IReadOnlyList<MemoryEntry>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<MemoryEntry?> FindExactAsync(string userId, string key, string value, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
