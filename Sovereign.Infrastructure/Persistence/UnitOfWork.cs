
using Sovereign.Application.Interfaces;

namespace Sovereign.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly SovereignDbContext _context;

    public UnitOfWork(SovereignDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }
}
