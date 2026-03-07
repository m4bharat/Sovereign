using Sovereign.Domain.Entities;

namespace Sovereign.Application.Interfaces;

public interface IConversationThreadRepository
{
    Task<ConversationThread?> GetByIdAsync(Guid threadId, CancellationToken ct = default);
    Task<ConversationThread?> GetByUserAndContactAsync(string userId, string contactId, CancellationToken ct = default);
    Task AddAsync(ConversationThread thread, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
