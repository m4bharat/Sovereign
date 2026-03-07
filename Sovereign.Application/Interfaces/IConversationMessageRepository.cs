using Sovereign.Domain.Entities;

namespace Sovereign.Application.Interfaces;

public interface IConversationMessageRepository
{
    Task AddAsync(ConversationMessage message, CancellationToken ct = default);
    Task<IReadOnlyList<ConversationMessage>> GetRecentByThreadIdAsync(Guid threadId, int take, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
