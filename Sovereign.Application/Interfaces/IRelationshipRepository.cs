using Sovereign.Domain.Aggregates;

namespace Sovereign.Application.Interfaces;

public interface IRelationshipRepository
{
    Task<Relationship?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Relationship>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task AddAsync(Relationship relationship, CancellationToken ct = default);
    Task UpdateAsync(Relationship relationship, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
