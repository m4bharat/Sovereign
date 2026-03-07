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

    public ConversationContextAssembler(
        IConversationThreadRepository threadRepository,
        IConversationMessageRepository messageRepository,
        IConversationSummaryRepository summaryRepository)
    {
        _threadRepository = threadRepository;
        _messageRepository = messageRepository;
        _summaryRepository = summaryRepository;
    }

    public async Task<MessageContext> AssembleAsync(AssembleAiContextRequest request, CancellationToken ct = default)
    {
        var thread = await _threadRepository.GetByUserAndContactAsync(request.UserId, request.ContactId, ct);

        if (thread is null)
        {
            return new MessageContext
            {
                UserId = request.UserId,
                ContactId = request.ContactId,
                Message = request.Message,
                RelationshipRole = request.RelationshipRole,
                RecentSummary = string.Empty,
                LastTopicSummary = string.Empty
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
            LastTopicSummary = latestSummary?.SummaryText ?? string.Empty
        };
    }

    private static string BuildRecentSummary(IReadOnlyList<Sovereign.Domain.Entities.ConversationMessage> messages)
    {
        if (messages.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        foreach (var message in messages.OrderBy(x => x.SentAtUtc))
        {
            sb.Append('[').Append(message.SenderType).Append("] ").Append(message.Content).AppendLine();
        }

        return sb.ToString().Trim();
    }
}
