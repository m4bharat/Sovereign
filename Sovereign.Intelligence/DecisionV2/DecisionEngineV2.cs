using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sovereign.Intelligence.Clients;
using Sovereign.Intelligence.Prompts;

namespace Sovereign.Intelligence.DecisionV2;

public sealed class DecisionEngineV2 : IDecisionEngineV2
{
    private readonly ILlmClient _llmClient;
    private readonly DecisionV2PromptBuilder _promptBuilder;
    private readonly ILogger<DecisionEngineV2> _logger;

    public DecisionEngineV2(
        ILlmClient llmClient,
        DecisionV2PromptBuilder promptBuilder,
        ILogger<DecisionEngineV2> logger)
    {
        _llmClient = llmClient;
        _promptBuilder = promptBuilder;
        _logger = logger;
    }

    public async Task<DecisionV2Result> DecideAsync(
        DecisionV2Input input,
        CancellationToken cancellationToken = default)
    {
        var strategy = ResolveStrategy(input);
        var tone = ResolveTone(input);
        var confidence = ResolveConfidence(input, strategy);

        var prompt = _promptBuilder.Build(input, strategy, tone, confidence);

        try
        {
            var raw = await _llmClient.CompleteAsync(prompt, cancellationToken);
            var parsed = Parse(raw, input, strategy, tone, confidence);

            if (IsWeak(parsed, input))
            {
                var fallback = BuildDeterministicFallback(input, strategy, tone, confidence);
                _logger.LogInformation(
                    "DecisionV2 returned weak output for user {UserId}; using fallback strategy {Strategy}.",
                    input.UserId,
                    fallback.Strategy);
                return fallback;
            }

            _logger.LogInformation(
                "DecisionV2 completed for user {UserId} with strategy {Strategy} and confidence {Confidence}.",
                input.UserId,
                parsed.Strategy,
                parsed.Confidence);

            return parsed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "DecisionV2 failed for user {UserId}; using deterministic fallback.",
                input.UserId);

            return BuildDeterministicFallback(input, strategy, tone, confidence);
        }
    }

    private static string ResolveStrategy(DecisionV2Input input)
    {
        var source = FirstNonEmpty(input.SourceText, input.ParentContextText).ToLowerInvariant();
        var draft = (input.Message ?? string.Empty).ToLowerInvariant();

        if (source.Contains("starting a new position") ||
            source.Contains("happy to share") ||
            source.Contains("excited to share") ||
            source.Contains("new position"))
        {
            return "celebratory_congratulations";
        }

        if (source.Contains("hiring") || source.Contains("we're hiring") || source.Contains("we are hiring"))
        {
            return "supportive_interest";
        }

        if (source.Contains("opinion") ||
            source.Contains("i believe") ||
            source.Contains("the real problem") ||
            source.Contains("leadership") ||
            source.Contains("empowerment") ||
            source.Contains("strategy"))
        {
            return "add_insight";
        }

        if (draft.Contains("?"))
        {
            return "engage_with_question";
        }

        if (!string.IsNullOrWhiteSpace(source))
        {
            return "contextual_reply";
        }

        return "generic_improve";
    }

    private static string ResolveTone(DecisionV2Input input)
    {
        if (string.Equals(input.Platform, "linkedin", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(input.RelationshipRole, "Peer", StringComparison.OrdinalIgnoreCase))
            {
                return "professional_peer";
            }

            return "professional_neutral";
        }

        return "neutral";
    }

    private static double ResolveConfidence(DecisionV2Input input, string strategy)
    {
        var score = 0.45;

        if (!string.IsNullOrWhiteSpace(input.SourceText)) score += 0.20;
        if (!string.IsNullOrWhiteSpace(input.ParentContextText)) score += 0.10;
        if (!string.IsNullOrWhiteSpace(input.SourceAuthor)) score += 0.05;
        if (!string.IsNullOrWhiteSpace(input.Message)) score += 0.10;

        if (strategy is "celebratory_congratulations" or "add_insight" or "contextual_reply")
        {
            score += 0.05;
        }

        return Math.Min(score, 0.95);
    }

    private static DecisionV2Result Parse(
        string raw,
        DecisionV2Input input,
        string strategy,
        string tone,
        double confidence)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return BuildDeterministicFallback(input, strategy, tone, confidence);
        }

        try
        {
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                return new DecisionV2Result
                {
                    Strategy = ReadString(root, "strategy", strategy),
                    Tone = ReadString(root, "tone", tone),
                    Confidence = ReadDouble(root, "confidence", confidence),
                    Reply = ReadString(root, "reply", string.Empty)
                };
            }
        }
        catch
        {
            // Treat raw as plain text below.
        }

        return new DecisionV2Result
        {
            Strategy = strategy,
            Tone = tone,
            Confidence = confidence,
            Reply = raw.Trim()
        };
    }

    private static bool IsWeak(DecisionV2Result result, DecisionV2Input input)
    {
        if (result is null) return true;
        if (string.IsNullOrWhiteSpace(result.Reply)) return true;

        var normalized = result.Reply.Trim().ToLowerInvariant();

        if (normalized.Contains("strong point—especially around") ||
            normalized.Contains("consistent execution is usually what determines"))
        {
            return true;
        }

        if (string.Equals(result.Strategy, "celebratory_congratulations", StringComparison.OrdinalIgnoreCase) &&
            !normalized.Contains("congrat"))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(input.SourceText) &&
            normalized.Length < 8)
        {
            return true;
        }

        return false;
    }

    private static DecisionV2Result BuildDeterministicFallback(
        DecisionV2Input input,
        string strategy,
        string tone,
        double confidence)
    {
        return new DecisionV2Result
        {
            Strategy = strategy,
            Tone = tone,
            Confidence = confidence,
            Reply = BuildFallbackReply(input, strategy)
        };
    }

    private static string BuildFallbackReply(DecisionV2Input input, string strategy)
    {
        var author = input.SourceAuthor?.Trim();
        var draft = input.Message?.Trim();

        if (string.Equals(strategy, "celebratory_congratulations", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(author))
            {
                return $"Congratulations, {author}! Wishing you great success in your new role.";
            }

            return "Congratulations! Wishing you great success in your new role.";
        }

        if (string.Equals(strategy, "engage_with_question", StringComparison.OrdinalIgnoreCase))
        {
            return !string.IsNullOrWhiteSpace(draft)
                ? draft
                : "Thoughtful perspective—curious how you’ve seen this play out in practice?";
        }

        if (string.Equals(strategy, "add_insight", StringComparison.OrdinalIgnoreCase))
        {
            return !string.IsNullOrWhiteSpace(draft)
                ? draft
                : "Thoughtful perspective—what stood out to me most is how execution determines whether ideas actually create impact.";
        }

        return !string.IsNullOrWhiteSpace(draft)
            ? draft
            : "Well said.";
    }

    private static string ReadString(JsonElement root, string propertyName, string fallback)
    {
        if (root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString()?.Trim() ?? fallback;
        }

        return fallback;
    }

    private static double ReadDouble(JsonElement root, string propertyName, double fallback)
    {
        if (root.TryGetProperty(propertyName, out var value) && value.TryGetDouble(out var parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private static string FirstNonEmpty(params string[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }
}