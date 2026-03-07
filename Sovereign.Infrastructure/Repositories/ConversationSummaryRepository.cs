using Microsoft.EntityFrameworkCore;
using Sovereign.Application.Interfaces;
using Sovereign.Domain.Entities;
using Sovereign.Infrastructure.Persistence;

namespace Sovereign.Infrastructure.Repositories;

public sealed class ConversationSummaryRepository : IConversationSummaryRepository
{
    private readonly SovereignDbContext _dbContext;

    public ConversationSummaryRepository(SovereignDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ConversationSummary?> GetLatestByThreadIdAsync(Guid threadId, CancellationToken ct = default)
        => await _dbContext.Set<ConversationSummary>()
            .Where(x => x.ThreadId == threadId)
            .OrderByDescending(x => x.GeneratedAtUtc)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(ConversationSummary summary, CancellationToken ct = default)
        => await _dbContext.Set<ConversationSummary>().AddAsync(summary, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _dbContext.SaveChangesAsync(ct);
}
