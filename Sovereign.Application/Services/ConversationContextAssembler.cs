using System.Text;
using Sovereign.Domain.DTOs;
using Sovereign.Domain.Services;
using Sovereign.Domain.Models;
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

    public ConversationContextAssembler(
        IConversationThreadRepository threadRepository,
        IConversationMessageRepository messageRepository,
        IConversationSummaryRepository summaryRepository,
        IMemoryRepository memoryRepository,
        MemorySimilarityService memorySimilarityService)
    {
        _threadRepository = threadRepository;
        _messageRepository = messageRepository;
        _summaryRepository = summaryRepository;
        _memoryRepository = memoryRepository;
        _memorySimilarityService = memorySimilarityService;
    }

    public async Task<MessageContext> AssembleAsync(
        AssembleAiContextRequest request,
        CancellationToken ct = default)
    {
        var interactionMode = ResolveInteractionMode(request);

        var thread = await _threadRepository.GetByUserAndContactAsync(
            request.UserId,
            request.ContactId,
            ct);

        var allMemories = await _memoryRepository.GetByUserIdAsync(request.UserId, ct);

        var memorySearchText = BuildMemorySearchText(request, interactionMode);
        var relevantMemoryMatches = _memorySimilarityService.Search(allMemories, memorySearchText, 5);

        var relevantMemoriesText = BuildRelevantMemories(relevantMemoryMatches);
        var memoryFacts = relevantMemoryMatches
            .Select(x => $"{x.Entry.Key}: {x.Entry.Value}")
            .ToArray();

        if (thread is null)
        {
            return new MessageContext
            {
                UserId = request.UserId,
                ContactId = request.ContactId,
                Message = NormalizeUserMessage(request.Message),
                RelationshipRole = request.RelationshipRole,
                RecentSummary = string.Empty,
                LastTopicSummary = string.Empty,
                RelevantMemories = interactionMode == "reply" ? string.Empty : relevantMemoriesText,
                MemoryFacts = interactionMode == "reply" ? Array.Empty<string>() : memoryFacts,
                RecentMessages = Array.Empty<string>(),
                Platform = request.Platform,
                Surface = request.Surface,
                CurrentUrl = request.CurrentUrl,
                SourceAuthor = request.SourceAuthor,
                SourceTitle = request.SourceTitle,
                SourceText = request.SourceText,
                ParentContextText = request.ParentContextText,
                NearbyContextText = TrimForReplyMode(request.NearbyContextText, interactionMode),
                InteractionMode = interactionMode,
                InteractionMetadata = new Dictionary<string, string>(request.InteractionMetadata)
            };
        }

        var latestSummary = await _summaryRepository.GetLatestByThreadIdAsync(thread.Id, ct);
        var recentMessages = await _messageRepository.GetRecentByThreadIdAsync(thread.Id, 8, ct);

        var recentMessageLines = recentMessages
            .OrderByDescending(x => x.SentAtUtc)
            .Take(8)
            .OrderBy(x => x.SentAtUtc)
            .Select(x => $"{x.SenderType}: {x.Content}")
            .ToArray();

        return new MessageContext
        {
            UserId = request.UserId,
            ContactId = request.ContactId,
            Message = NormalizeUserMessage(request.Message),
            RelationshipRole = request.RelationshipRole,

            // For reply mode, reduce thread/history dominance.
            RecentSummary = interactionMode == "reply"
                ? string.Empty
                : BuildRecentSummary(recentMessages),

            LastTopicSummary = interactionMode == "reply"
                ? string.Empty
                : latestSummary?.SummaryText ?? string.Empty,

            RelevantMemories = interactionMode == "reply"
                ? string.Empty
                : relevantMemoriesText,

            MemoryFacts = interactionMode == "reply"
                ? Array.Empty<string>()
                : memoryFacts,

            RecentMessages = interactionMode == "chat"
                ? recentMessageLines
                : Array.Empty<string>(),

            Platform = request.Platform,
            Surface = request.Surface,
            CurrentUrl = request.CurrentUrl,
            SourceAuthor = request.SourceAuthor,
            SourceTitle = request.SourceTitle,
            SourceText = request.SourceText,
            ParentContextText = interactionMode == "chat" ? request.ParentContextText : string.Empty,
            NearbyContextText = TrimForReplyMode(request.NearbyContextText, interactionMode),
            InteractionMode = interactionMode,
            InteractionMetadata = new Dictionary<string, string>(request.InteractionMetadata)
        };
    }

    private static string ResolveInteractionMode(AssembleAiContextRequest request)
    {
        var surface = (request.Surface ?? string.Empty).Trim().ToLowerInvariant();

        // Surface is the strongest signal.
        if (surface is "messaging_chat" or "chatbox" or "dm_chat" or "linkedin_chat")
            return "chat";

        if (surface is "feed_reply" or "comment_reply" or "reply" or "add_comment")
            return "reply";

        if (surface is "start_post" or "create_post" or "compose_post" or "write_post")
            return "compose";

        // Content fallback when surface is missing or noisy.
        if (!string.IsNullOrWhiteSpace(request.SourceText))
            return "reply";

        if (!string.IsNullOrWhiteSpace(request.ParentContextText))
            return "chat";

        return "compose";
    }

    private static string NormalizeUserMessage(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var text = input.Trim();

        text = text.Replace("\r\n", "\n");
        text = text.Replace("\t", " ");

        while (text.Contains("  "))
            text = text.Replace("  ", " ");

        return text;
    }

    private static string BuildMemorySearchText(
        AssembleAiContextRequest request,
        string interactionMode)
    {
        if (interactionMode == "reply")
        {
            var replyParts = new[]
            {
                request.SourceText,
                request.Message,
                request.SourceAuthor,
                request.SourceTitle
            };

            return string.Join(" ", replyParts.Where(part => !string.IsNullOrWhiteSpace(part)));
        }

        if (interactionMode == "chat")
        {
            var chatParts = new[]
            {
                request.Message,
                request.ParentContextText,
                request.NearbyContextText
            };

            return string.Join(" ", chatParts.Where(part => !string.IsNullOrWhiteSpace(part)));
        }

        return request.Message;
    }

    private static string TrimForReplyMode(string nearbyContextText, string interactionMode)
    {
        if (interactionMode != "reply" || string.IsNullOrWhiteSpace(nearbyContextText))
        {
            return nearbyContextText;
        }

        var lines = nearbyContextText
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(6);

        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildRecentSummary(
        IReadOnlyList<Sovereign.Domain.Entities.ConversationMessage> messages)
    {
        if (messages.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        foreach (var message in messages
                     .OrderByDescending(x => x.SentAtUtc)
                     .Take(8)
                     .OrderBy(x => x.SentAtUtc))
        {
            var content = message.Content.Length > 240
                ? message.Content[..240]
                : message.Content;

            sb.Append('[')
              .Append(message.SenderType)
              .Append("] ")
              .Append(content)
              .AppendLine();
        }

        return sb.ToString().Trim();
    }

    private static string BuildRelevantMemories(
        IReadOnlyList<(Sovereign.Domain.Entities.MemoryEntry Entry, double Score)> memories)
    {
        if (memories.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(
            Environment.NewLine,
            memories.Select(memory =>
                $"- {memory.Entry.Key}: {memory.Entry.Value} (score:{memory.Score:0.00})"));
    }
}