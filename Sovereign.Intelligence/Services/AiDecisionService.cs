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

    public AiDecisionService(
        ILlmClient llmClient,
        AiDecisionPromptBuilder promptBuilder,
        AiDecisionJsonParser parser,
        ILogger<AiDecisionService> logger)
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

            if (IsWeakResult(parsed))
            {
                var fallback = BuildDeterministicFallback(context);
                _logger.LogInformation(
                    "AI decision returned weak output for user {UserId} contact {ContactId}; using deterministic fallback action {Action}.",
                    SafeGet(context, "UserId"),
                    SafeGet(context, "ContactId"),
                    fallback.Action);

                return fallback;
            }

            _logger.LogInformation(
                "AI decision completed for user {UserId} contact {ContactId} with action {Action} and confidence {Confidence}.",
                SafeGet(context, "UserId"),
                SafeGet(context, "ContactId"),
                parsed.Action,
                parsed.Confidence);

            return parsed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "AI decision failed for user {UserId} contact {ContactId}; returning deterministic fallback.",
                SafeGet(context, "UserId"),
                SafeGet(context, "ContactId"));

            return BuildDeterministicFallback(context);
        }
    }

    private static bool IsWeakResult(AiDecision decision)
    {
        var placeholderSummary =
            "Conversation summary requested. Local mode cannot generate a richer summary yet.";

        return string.Equals(decision.Summary?.Trim(), placeholderSummary, StringComparison.OrdinalIgnoreCase) ||
               (string.IsNullOrWhiteSpace(decision.Reply) && string.IsNullOrWhiteSpace(decision.Summary)) ||
               (string.Equals(decision.Action, AiDecision.SummarizeAction, StringComparison.OrdinalIgnoreCase)
                   && string.IsNullOrWhiteSpace(decision.Reply));
    }

    private static AiDecision BuildDeterministicFallback(MessageContext context)
    {
        var input = ExtractMessage(context);
        var platform = SafeGet(context, "Platform", "LinkedIn");
        var relationshipRole = SafeGet(context, "RelationshipRole", "Peer");

        if (LooksLikeSummaryRequest(input))
        {
            return new AiDecision
            {
                Action = AiDecision.SummarizeAction,
                Summary = BuildSummary(input),
                Confidence = 0.64
            };
        }

        return new AiDecision
        {
            Action = AiDecision.ReplyAction,
            Reply = BuildRewrite(input, platform, relationshipRole),
            Summary = string.Empty,
            Confidence = 0.74
        };
    }

    private static bool LooksLikeSummaryRequest(string input)
    {
        var text = input.ToLowerInvariant();
        return text.Contains("summarize") ||
               text.Contains("summary") ||
               text.Contains("recap");
    }

    private static string BuildSummary(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "No content was provided to summarize.";
        }

        var trimmed = input.Trim();
        return trimmed.Length <= 220
            ? trimmed
            : trimmed[..220].TrimEnd() + "...";
    }

    private static string BuildRewrite(string input, string platform, string relationshipRole)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "Strong point. The key is to focus on clear value, practical execution, and real-world impact.";
        }

        var cleaned = input.Trim();

        if (string.Equals(platform, "LinkedIn", StringComparison.OrdinalIgnoreCase))
        {
            return $"Strong point. {cleaned} The real advantage comes from embedding intelligence into the workflow so the product becomes genuinely useful in day-to-day execution.";
        }

        if (string.Equals(relationshipRole, "Peer", StringComparison.OrdinalIgnoreCase))
        {
            return $"That makes sense. {cleaned} I’d frame it in a way that is direct, clear, and easy for the other person to respond to.";
        }

        return $"Here’s a stronger version: {cleaned}";
    }

    private static string ExtractMessage(MessageContext context)
    {
        var explicitMessage = SafeGet(context, "Message");
        if (!string.IsNullOrWhiteSpace(explicitMessage))
        {
            return explicitMessage;
        }

        return SafeGet(context, "RawInput");
    }

    private static string SafeGet(object target, string propertyName, string fallback = "")
    {
        var property = target.GetType().GetProperty(propertyName);
        var value = property?.GetValue(target);
        return value?.ToString() ?? fallback;
    }
}
