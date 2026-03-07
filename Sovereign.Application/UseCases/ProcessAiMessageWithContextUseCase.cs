using Sovereign.Application.DTOs;
using Sovereign.Application.Interfaces;
using Sovereign.Application.Services;
using Sovereign.Domain.Entities;
using Sovereign.Intelligence.Services;

namespace Sovereign.Application.UseCases;

public sealed class ProcessAiMessageWithContextUseCase
{
    private readonly IAiDecisionService _aiDecisionService;
    private readonly IMemoryRepository _memoryRepository;
    private readonly IConversationContextAssembler _contextAssembler;

    public ProcessAiMessageWithContextUseCase(
        IAiDecisionService aiDecisionService,
        IMemoryRepository memoryRepository,
        IConversationContextAssembler contextAssembler)
    {
        _aiDecisionService = aiDecisionService;
        _memoryRepository = memoryRepository;
        _contextAssembler = contextAssembler;
    }

    public async Task<AiDecisionResponse> ExecuteAsync(AssembleAiContextRequest request, CancellationToken ct = default)
    {
        var context = await _contextAssembler.AssembleAsync(request, ct);
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
