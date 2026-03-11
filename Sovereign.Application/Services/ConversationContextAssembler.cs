using System.Text;
using Sovereign.Application.DTOs;
using Sovereign.Application.Interfaces;
using Sovereign.Intelligence.Models;

namespace Sovereign.Application.Services;

public sealed class ConversationContextAssembler : IConversationContextAssembler
{
    private readonly IConversationThreadRepository _threadRepository;
    private readonly IConversationMessageRepository _messageRepository;
    private readonly IConversationSummaryRepository _summaryRepository;
    private readonly IMemoryRepository _memoryRepository;
    private readonly MemorySimilarityService _memorySimilarityService;

    public ConversationContextAssembler(IConversationThreadRepository threadRepository, IConversationMessageRepository messageRepository, IConversationSummaryRepository summaryRepository, IMemoryRepository memoryRepository, MemorySimilarityService memorySimilarityService)
    {
        _threadRepository = threadRepository;
        _messageRepository = messageRepository;
        _summaryRepository = summaryRepository;
        _memoryRepository = memoryRepository;
        _memorySimilarityService = memorySimilarityService;
    }

    public async Task<MessageContext> AssembleAsync(AssembleAiContextRequest request, CancellationToken ct = default)
    {
        var thread = await _threadRepository.GetByUserAndContactAsync(request.UserId, request.ContactId, ct);
        var allMemories = await _memoryRepository.GetByUserIdAsync(request.UserId, ct);
        var relevantMemories = BuildRelevantMemories(_memorySimilarityService.Search(allMemories, request.Message, 5));

        if (thread is null)
        {
            return new MessageContext
            {
                UserId = request.UserId,
                ContactId = request.ContactId,
                Message = request.Message,
                RelationshipRole = request.RelationshipRole,
                RecentSummary = string.Empty,
                LastTopicSummary = string.Empty,
                RelevantMemories = relevantMemories
            };
        }

        var latestSummary = await _summaryRepository.GetLatestByThreadIdAsync(thread.Id, ct);
        var recentMessages = await _messageRepository.GetRecentByThreadIdAsync(thread.Id, 8, ct);

        return new MessageContext
        {
            UserId = request.UserId,
            ContactId = request.ContactId,
            Message = request.Message,
            RelationshipRole = request.RelationshipRole,
            RecentSummary = BuildRecentSummary(recentMessages),
            LastTopicSummary = latestSummary?.SummaryText ?? string.Empty,
            RelevantMemories = relevantMemories
        };
    }

    private static string BuildRecentSummary(IReadOnlyList<Sovereign.Domain.Entities.ConversationMessage> messages)
    {
        if (messages.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var message in messages.OrderByDescending(x => x.SentAtUtc).Take(8).OrderBy(x => x.SentAtUtc))
        {
            var content = message.Content.Length > 240 ? message.Content[..240] : message.Content;
            sb.Append('[').Append(message.SenderType).Append("] ").Append(content).AppendLine();
        }

        return sb.ToString().Trim();
    }

    private static string BuildRelevantMemories(IReadOnlyList<(Sovereign.Domain.Entities.MemoryEntry Entry, double Score)> memories)
    {
        if (memories.Count == 0)
            return string.Empty;

        return string.Join(Environment.NewLine, memories.Select(memory => $"- {memory.Entry.Key}: {memory.Entry.Value} (score:{memory.Score:0.00})"));
    }
}
