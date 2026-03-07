using Sovereign.Domain.Entities;

namespace Sovereign.Application.Interfaces;

public interface IMemoryRepository
{
    Task AddAsync(MemoryEntry memory, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
