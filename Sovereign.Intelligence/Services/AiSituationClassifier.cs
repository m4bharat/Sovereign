using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sovereign.Domain.Models;
using Sovereign.Intelligence.Clients;
using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Services;

public sealed class AiSituationClassifier : IAiSituationClassifier
{
    private readonly ILlmClient _llmClient;
    private readonly ILogger<AiSituationClassifier> _logger;

    public AiSituationClassifier(
        ILlmClient llmClient,
        ILogger<AiSituationClassifier> logger)
    {
        _llmClient = llmClient;
        _logger = logger;
    }

    public async Task<AiSituationClassification?> ClassifyAsync(
        MessageContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var prompt = BuildPrompt(context);
            var raw = await _llmClient.CompleteAsync(prompt, cancellationToken);

            if (string.IsNullOrWhiteSpace(raw))
            {
                _logger.LogInformation("AI situation classifier returned an empty payload.");
                return null;
            }

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            var classification = new AiSituationClassification
            {
                SituationType = root.TryGetProperty("situationType", out var situationType)
                    ? situationType.GetString() ?? "general"
                    : "general",
                UserIntent = root.TryGetProperty("userIntent", out var userIntent)
                    ? userIntent.GetString() ?? "generate_reply"
                    : "generate_reply",
                RecommendedMove = root.TryGetProperty("recommendedMove", out var recommendedMove)
                    ? recommendedMove.GetString() ?? "engage"
                    : "engage",
                IsCommandOnly = root.TryGetProperty("isCommandOnly", out var isCommandOnly) &&
                                isCommandOnly.ValueKind is JsonValueKind.True or JsonValueKind.False
                    ? isCommandOnly.GetBoolean()
                    : false,
                Confidence = root.TryGetProperty("confidence", out var confidence) &&
                             confidence.TryGetDouble(out var confidenceValue)
                    ? confidenceValue
                    : 0.0,
                Rationale = root.TryGetProperty("rationale", out var rationale)
                    ? rationale.GetString() ?? string.Empty
                    : string.Empty
            };

            _logger.LogInformation(
                "AI situation classifier returned {SituationType} with confidence {Confidence:F2}.",
                classification.SituationType,
                classification.Confidence);

            return classification;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI situation classification failed. Falling back to deterministic detection.");
            return null;
        }
    }

    private static string BuildPrompt(MessageContext context)
    {
        var surface = context.Surface ?? string.Empty;
        var interactionMode = context.InteractionMode ?? string.Empty;
        var message = context.Message ?? string.Empty;
        var sourceTitle = context.SourceTitle ?? string.Empty;
        var sourceText = context.SourceText ?? string.Empty;
        var parent = context.ParentContextText ?? string.Empty;
        var nearby = context.NearbyContextText ?? string.Empty;

        return $$"""
Classify this LinkedIn interaction.

Do not generate reply text.
Return only valid JSON.

Expected JSON:
{
  "situationType": "achievement_share",
  "userIntent": "generate_comment",
  "recommendedMove": "congratulate",
  "isCommandOnly": true,
  "confidence": 0.96,
  "rationale": "Post announces joining Thoughtworks and a new chapter."
}

Context:
- Surface: {{surface}}
- Interaction mode: {{interactionMode}}
- User message: {{message}}
- Source title: {{sourceTitle}}
- Source text: {{sourceText}}
- Parent context: {{parent}}
- Nearby context: {{nearby}}

Rules:
- Distinguish command-only user messages from actual draft text.
- If the message is like "reply", "make a comment", or "write comment", treat that as command intent, not draft content.
- For job starts, promotions, new roles, new chapters, or milestone announcements, prefer "achievement_share".
- For compose requests to write a post, prefer "compose_post".
- For casual chat wishes like birthday messages, prefer a greeting/response style situation, not rewrite intent.
- Only classify. Do not write the final reply.
""";
    }
}
