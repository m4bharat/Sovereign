using Sovereign.Application.DTOs;
using Sovereign.Application.Interfaces;
using Sovereign.Domain.Entities;

namespace Sovereign.Application.UseCases;

public sealed class GenerateThreadSummaryUseCase
{
    private readonly IConversationThreadRepository _threadRepository;
    private readonly IConversationMessageRepository _messageRepository;
    private readonly IConversationSummaryRepository _summaryRepository;

    public GenerateThreadSummaryUseCase(
        IConversationThreadRepository threadRepository,
        IConversationMessageRepository messageRepository,
        IConversationSummaryRepository summaryRepository)
    {
        _threadRepository = threadRepository;
        _messageRepository = messageRepository;
        _summaryRepository = summaryRepository;
    }

    public async Task<GenerateThreadSummaryResponse> ExecuteAsync(Guid threadId, CancellationToken ct = default)
    {
        var thread = await _threadRepository.GetByIdAsync(threadId, ct)
            ?? throw new InvalidOperationException("Conversation thread not found.");

        var recentMessages = await _messageRepository.GetRecentByThreadIdAsync(thread.Id, 20, ct);

        var summaryText = recentMessages.Count == 0
            ? "No conversation messages available yet."
            : string.Join(" ", recentMessages
                .OrderBy(x => x.SentAtUtc)
                .TakeLast(6)
                .Select(x => $"{x.SenderType}: {x.Content}"));

        var summary = new ConversationSummary(Guid.NewGuid(), thread.Id, summaryText);

        await _summaryRepository.AddAsync(summary, ct);
        await _summaryRepository.SaveChangesAsync(ct);

        return new GenerateThreadSummaryResponse
        {
            ThreadId = thread.Id,
            Summary = summary.SummaryText
        };
    }
}
