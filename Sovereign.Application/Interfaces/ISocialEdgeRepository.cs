using Sovereign.Domain.Entities;

namespace Sovereign.Application.Interfaces;

public interface ISocialEdgeRepository
{
    Task<SocialEdge?> GetAsync(string sourceUserId, string targetContactId, CancellationToken ct = default);
    Task<IReadOnlyList<SocialEdge>> GetByUserAsync(string userId, CancellationToken ct = default);
    Task AddAsync(SocialEdge edge, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
