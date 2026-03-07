using Microsoft.EntityFrameworkCore;
using Sovereign.Application.Interfaces;
using Sovereign.Domain.Entities;
using Sovereign.Infrastructure.Persistence;

namespace Sovereign.Infrastructure.Repositories;

public sealed class ConversationThreadRepository : IConversationThreadRepository
{
    private readonly SovereignDbContext _dbContext;

    public ConversationThreadRepository(SovereignDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ConversationThread?> GetByIdAsync(Guid threadId, CancellationToken ct = default)
        => await _dbContext.Set<ConversationThread>().FirstOrDefaultAsync(x => x.Id == threadId, ct);

    public async Task<ConversationThread?> GetByUserAndContactAsync(string userId, string contactId, CancellationToken ct = default)
        => await _dbContext.Set<ConversationThread>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ContactId == contactId, ct);

    public async Task AddAsync(ConversationThread thread, CancellationToken ct = default)
        => await _dbContext.Set<ConversationThread>().AddAsync(thread, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _dbContext.SaveChangesAsync(ct);
}
