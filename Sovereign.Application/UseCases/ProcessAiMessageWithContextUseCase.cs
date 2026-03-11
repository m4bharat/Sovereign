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

    public ProcessAiMessageWithContextUseCase(IAiDecisionService aiDecisionService, IMemoryRepository memoryRepository, IConversationContextAssembler contextAssembler)
    {
        _aiDecisionService = aiDecisionService;
        _memoryRepository = memoryRepository;
        _contextAssembler = contextAssembler;
    }

    public async Task<AiDecisionResponse> ExecuteAsync(AssembleAiContextRequest request, CancellationToken ct = default)
    {
        var context = await _contextAssembler.AssembleAsync(request, ct);
        var decision = await _aiDecisionService.DecideAsync(context, ct);
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

    private async Task PersistMemoryIfEligibleAsync(string userId, AiDecision decision, CancellationToken ct)
    {
        if (!string.Equals(decision.Action, AiDecision.SaveMemoryAction, StringComparison.OrdinalIgnoreCase) || decision.Confidence < 0.80)
            return;
        if (string.IsNullOrWhiteSpace(decision.MemoryKey) || string.IsNullOrWhiteSpace(decision.MemoryValue))
            return;
        if (await _memoryRepository.FindExactAsync(userId, decision.MemoryKey, decision.MemoryValue, ct) is not null)
            return;

        var memory = new MemoryEntry(Guid.NewGuid(), userId, decision.MemoryKey, decision.MemoryValue);
        await _memoryRepository.AddAsync(memory, ct);
        await _memoryRepository.SaveChangesAsync(ct);
    }
}
