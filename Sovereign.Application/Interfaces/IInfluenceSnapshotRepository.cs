using Sovereign.Domain.Entities;

namespace Sovereign.Application.Interfaces;

public interface IInfluenceSnapshotRepository
{
    Task AddAsync(InfluenceSnapshot snapshot, CancellationToken ct = default);
    Task<IReadOnlyList<InfluenceSnapshot>> GetByUserAsync(string userId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
