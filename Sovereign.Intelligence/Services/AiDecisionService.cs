using Sovereign.Intelligence.Clients;
using Sovereign.Intelligence.Models;
using Sovereign.Intelligence.Parsers;
using Sovereign.Intelligence.Prompts;

namespace Sovereign.Intelligence.Services;

public sealed class AiDecisionService : IAiDecisionService
{
    private readonly ILlmClient _llmClient;
    private readonly AiDecisionPromptBuilder _promptBuilder;
    private readonly AiDecisionJsonParser _parser;

    public AiDecisionService(ILlmClient llmClient, AiDecisionPromptBuilder promptBuilder, AiDecisionJsonParser parser)
    {
        _llmClient = llmClient;
        _promptBuilder = promptBuilder;
        _parser = parser;
    }

    public async Task<AiDecision> DecideAsync(MessageContext context, CancellationToken ct = default)
    {
        var prompt = _promptBuilder.Build(context);
        var raw = await _llmClient.CompleteAsync(prompt, ct);
        return _parser.Parse(raw);
    }
}
