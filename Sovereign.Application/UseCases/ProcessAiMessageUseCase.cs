using Sovereign.Application.DTOs;
using Sovereign.Application.Interfaces;
using Sovereign.Domain.Entities;
using Sovereign.Intelligence.Models;
using Sovereign.Intelligence.Services;

namespace Sovereign.Application.UseCases;

public sealed class ProcessAiMessageUseCase
{
    private readonly IAiDecisionService _aiDecisionService;
    private readonly IMemoryRepository _memoryRepository;

    public ProcessAiMessageUseCase(IAiDecisionService aiDecisionService, IMemoryRepository memoryRepository)
    {
        _aiDecisionService = aiDecisionService;
        _memoryRepository = memoryRepository;
    }

    public async Task<AiDecisionResponse> ExecuteAsync(AiDecisionRequest request, CancellationToken ct = default)
    {
        var context = new MessageContext
        {
            UserId = request.UserId,
            ContactId = request.ContactId,
            Message = request.Message,
            RelationshipRole = request.RelationshipRole,
            RecentSummary = request.RecentSummary,
            LastTopicSummary = request.LastTopicSummary
        };

        var decision = await _aiDecisionService.DecideAsync(context, ct);

        if (string.Equals(decision.Action, "save_memory", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(decision.MemoryKey)
            && !string.IsNullOrWhiteSpace(decision.MemoryValue))
        {
            var memory = new MemoryEntry(Guid.NewGuid(), request.UserId, decision.MemoryKey, decision.MemoryValue);
            await _memoryRepository.AddAsync(memory, ct);
            await _memoryRepository.SaveChangesAsync(ct);
        }

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
}
