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

            var json = ExtractJsonObject(raw);
            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogInformation("AI situation classifier returned a payload without a JSON object.");
                return null;
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var classification = new AiSituationClassification
            {
                SituationType = NormalizeSituationType(TryGetString(root, "situationType"), context),
                UserIntent = NormalizeUserIntent(TryGetString(root, "userIntent")),
                RecommendedMove = NormalizeRecommendedMove(TryGetString(root, "recommendedMove")),
                IsCommandOnly = TryGetBoolean(root, "isCommandOnly") ?? false,
                Confidence = Clamp01(TryGetDouble(root, "confidence") ?? 0.0),
                Rationale = TryGetString(root, "rationale") ?? string.Empty
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

    private static string? ExtractJsonObject(string text)
    {
        var trimmed = (text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return null;

        var start = trimmed.IndexOf('{');
        if (start < 0)
            return null;

        var depth = 0;
        var inString = false;
        var escaped = false;

        for (var i = start; i < trimmed.Length; i++)
        {
            var ch = trimmed[i];

            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (ch == '\\')
            {
                escaped = true;
                continue;
            }

            if (ch == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString)
                continue;

            if (ch == '{')
                depth++;
            else if (ch == '}')
                depth--;

            if (depth == 0)
                return trimmed[start..(i + 1)];
        }

        return null;
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return null;

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            JsonValueKind.True => bool.TrueString.ToLowerInvariant(),
            JsonValueKind.False => bool.FalseString.ToLowerInvariant(),
            _ => null
        };
    }

    private static double? TryGetDouble(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return null;

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetDouble(out var value) => value,
            JsonValueKind.String when double.TryParse(property.GetString(), out var value) => value,
            _ => null
        };
    }

    private static bool? TryGetBoolean(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return null;

        return property.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(property.GetString(), out var value) => value,
            _ => null
        };
    }

    private static string NormalizeSituationType(string? value, MessageContext context)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
            return InferSituationFallback(context);

        return normalized switch
        {
            "achievement" => "achievement_share",
            "achievement_post" => "achievement_share",
            "compose" => "compose_post",
            "composepost" => "compose_post",
            "draft_post" => "compose_post",
            "dm" => "direct_message",
            "directmessage" => "direct_message",
            "direct message" => "direct_message",
            "question_post" => "question",
            "question_reply" => "question",
            "thought_leadership" => "industry_news",
            _ => normalized
        };
    }

    private static string NormalizeUserIntent(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
            return "generate_reply";

        return normalized switch
        {
            "reply" => "generate_reply",
            "comment" => "generate_comment",
            "generatecomment" => "generate_comment",
            "compose" => "compose_post",
            "post" => "compose_post",
            _ => normalized
        };
    }

    private static string NormalizeRecommendedMove(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
            return "engage";

        return normalized switch
        {
            "reply" => "engage",
            "comment" => "engage",
            "insight" => "add_insight",
            "question" => "ask_relevant_question",
            "compose" => "draft_post",
            "compose_post" => "draft_post",
            "rewrite" => "rewrite_user_intent",
            _ => normalized
        };
    }

    private static string InferSituationFallback(MessageContext context)
    {
        var surface = (context.Surface ?? string.Empty).Trim().ToLowerInvariant();
        return surface switch
        {
            "start_post" => "compose_post",
            "messaging_chat" => "direct_message",
            _ => "general"
        };
    }

    private static double Clamp01(double value)
    {
        return Math.Clamp(value, 0.0, 1.0);
    }
}
