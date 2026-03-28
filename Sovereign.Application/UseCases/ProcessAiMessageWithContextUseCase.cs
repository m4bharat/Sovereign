using Sovereign.Application.DTOs;
using Sovereign.Application.Interfaces;
using Sovereign.Application.Services;
using Sovereign.Domain.Entities;
using Sovereign.Intelligence.Models;
using Sovereign.Intelligence.Services;

namespace Sovereign.Application.UseCases;

public sealed class ProcessAiMessageWithContextUseCase
{
    private readonly IAiDecisionService _aiDecisionService;
    private readonly IMemoryRepository _memoryRepository;
    private readonly IConversationContextAssembler _contextAssembler;
    private readonly IConversationThreadRepository _threadRepository;
    private readonly IConversationMessageRepository _messageRepository;

    public ProcessAiMessageWithContextUseCase(
        IAiDecisionService aiDecisionService,
        IMemoryRepository memoryRepository,
        IConversationContextAssembler contextAssembler,
        IConversationThreadRepository threadRepository,
        IConversationMessageRepository messageRepository)
    {
        _aiDecisionService = aiDecisionService;
        _memoryRepository = memoryRepository;
        _contextAssembler = contextAssembler;
        _threadRepository = threadRepository;
        _messageRepository = messageRepository;
    }

    public async Task<AiDecisionResponse> ExecuteAsync(
        AssembleAiContextRequest request,
        CancellationToken ct = default)
    {
        var thread = await GetOrCreateThreadAsync(request, ct);

        await SaveUserMessageAsync(thread.Id, request.Message, ct);

        var context = await _contextAssembler.AssembleAsync(request, ct);
        var decision = await _aiDecisionService.DecideAsync(context, ct);

        await SaveAssistantMessageAsync(thread.Id, decision, ct);
        await PersistMemoryIfEligibleAsync(request.UserId, decision, ct);

        return new AiDecisionResponse
        {
            Action = decision.Action,
            Reply = decision.Reply,
            MemoryKey = decision.MemoryKey,
            MemoryValue = decision.MemoryValue,
            Summary = decision.Summary,
            Confidence = decision.Confidence
        };
    }

    private async Task<ConversationThread> GetOrCreateThreadAsync(
        AssembleAiContextRequest request,
        CancellationToken ct)
    {
        var existingThread = await _threadRepository.GetByUserAndContactAsync(
            request.UserId,
            request.ContactId,
            ct);

        if (existingThread is not null)
        {
            existingThread.Touch();
            await _threadRepository.SaveChangesAsync(ct);
            return existingThread;
        }

        var title = BuildThreadTitle(request);
        var newThread = new ConversationThread(
            Guid.NewGuid(),
            request.UserId,
            request.ContactId,
            title);

        await _threadRepository.AddAsync(newThread, ct);
        await _threadRepository.SaveChangesAsync(ct);

        return newThread;
    }

    private async Task SaveUserMessageAsync(Guid threadId, string message, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var inbound = new ConversationMessage(
            Guid.NewGuid(),
            threadId,
            "User",
            message.Trim());

        await _messageRepository.AddAsync(inbound, ct);
        await _messageRepository.SaveChangesAsync(ct);
    }

    private async Task SaveAssistantMessageAsync(Guid threadId, AiDecision decision, CancellationToken ct)
    {
        var content = !string.IsNullOrWhiteSpace(decision.Reply)
            ? decision.Reply.Trim()
            : !string.IsNullOrWhiteSpace(decision.Summary)
                ? decision.Summary.Trim()
                : $"Action: {decision.Action}";

        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        var outbound = new ConversationMessage(
            Guid.NewGuid(),
            threadId,
            "Assistant",
            content);

        await _messageRepository.AddAsync(outbound, ct);
        await _messageRepository.SaveChangesAsync(ct);
    }

    private async Task PersistMemoryIfEligibleAsync(
        string userId,
        AiDecision decision,
        CancellationToken ct)
    {
        if (!string.Equals(
                decision.Action,
                AiDecision.SaveMemoryAction,
                StringComparison.OrdinalIgnoreCase) ||
            decision.Confidence < 0.80)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(decision.MemoryKey) ||
            string.IsNullOrWhiteSpace(decision.MemoryValue))
        {
            return;
        }

        if (await _memoryRepository.FindExactAsync(
                userId,
                decision.MemoryKey,
                decision.MemoryValue,
                ct) is not null)
        {
            return;
        }

        var memory = new MemoryEntry(
            Guid.NewGuid(),
            userId,
            decision.MemoryKey,
            decision.MemoryValue);

        await _memoryRepository.AddAsync(memory, ct);
        await _memoryRepository.SaveChangesAsync(ct);
    }

    private static string BuildThreadTitle(AssembleAiContextRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.ContactId))
        {
            return $"Conversation with {request.ContactId}";
        }

        return "Untitled conversation";
    }
}
