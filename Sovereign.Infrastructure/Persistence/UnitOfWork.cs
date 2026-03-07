using Sovereign.Application.Interfaces;

namespace Sovereign.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly SovereignDbContext _dbContext;

    public UnitOfWork(SovereignDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _dbContext.SaveChangesAsync(ct);
}
