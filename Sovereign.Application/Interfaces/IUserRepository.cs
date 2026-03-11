using Sovereign.Domain.Entities;

namespace Sovereign.Application.Interfaces;

public interface IUserRepository
{
    Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserAccount?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(UserAccount user, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
