using Sovereign.Domain.Entities;

namespace Sovereign.Application.Interfaces;

public interface IConversationSummaryRepository
{
    Task<ConversationSummary?> GetLatestByThreadIdAsync(Guid threadId, CancellationToken ct = default);
    Task AddAsync(ConversationSummary summary, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
