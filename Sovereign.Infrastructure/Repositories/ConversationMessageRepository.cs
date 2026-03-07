using Microsoft.EntityFrameworkCore;
using Sovereign.Application.Interfaces;
using Sovereign.Domain.Entities;
using Sovereign.Infrastructure.Persistence;

namespace Sovereign.Infrastructure.Repositories;

public sealed class ConversationMessageRepository : IConversationMessageRepository
{
    private readonly SovereignDbContext _dbContext;

    public ConversationMessageRepository(SovereignDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ConversationMessage message, CancellationToken ct = default)
        => await _dbContext.Set<ConversationMessage>().AddAsync(message, ct);

    public async Task<IReadOnlyList<ConversationMessage>> GetRecentByThreadIdAsync(Guid threadId, int take, CancellationToken ct = default)
        => await _dbContext.Set<ConversationMessage>()
            .Where(x => x.ThreadId == threadId)
            .OrderByDescending(x => x.SentAtUtc)
            .Take(take)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _dbContext.SaveChangesAsync(ct);
}
