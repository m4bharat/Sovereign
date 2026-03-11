using Microsoft.EntityFrameworkCore;
using Sovereign.Application.Interfaces;
using Sovereign.Domain.Entities;
using Sovereign.Infrastructure.Persistence;

namespace Sovereign.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly SovereignDbContext _dbContext;

    public UserRepository(SovereignDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _dbContext.Set<UserAccount>().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<UserAccount?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _dbContext.Set<UserAccount>().FirstOrDefaultAsync(x => x.Email == email, ct);

    public async Task AddAsync(UserAccount user, CancellationToken ct = default)
        => await _dbContext.Set<UserAccount>().AddAsync(user, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _dbContext.SaveChangesAsync(ct);
}
