using Microsoft.Extensions.Logging;
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
    private readonly ILogger<AiDecisionService> _logger;

    public AiDecisionService(ILlmClient llmClient, AiDecisionPromptBuilder promptBuilder, AiDecisionJsonParser parser, ILogger<AiDecisionService> logger)
    {
        _llmClient = llmClient;
        _promptBuilder = promptBuilder;
        _parser = parser;
        _logger = logger;
    }

    public async Task<AiDecision> DecideAsync(MessageContext context, CancellationToken ct = default)
    {
        var prompt = _promptBuilder.Build(context);

        try
        {
            var raw = await _llmClient.CompleteAsync(prompt, ct);
            var parsed = _parser.Parse(raw);

            _logger.LogInformation("AI decision completed for user {UserId} contact {ContactId} with action {Action} and confidence {Confidence}", context.UserId, context.ContactId, parsed.Action, parsed.Confidence);
            return parsed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI decision failed for user {UserId} contact {ContactId}; returning deterministic fallback.", context.UserId, context.ContactId);
            return new AiDecision
            {
                Action = AiDecision.ReplyAction,
                Reply = "I understand. Could you share a little more context so I can help properly?",
                Confidence = 0.20
            };
        }
    }
}
