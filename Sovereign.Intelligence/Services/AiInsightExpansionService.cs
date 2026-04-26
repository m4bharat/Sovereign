using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Sovereign.Domain.Models;
using Sovereign.Intelligence.Clients;
using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Services;

public sealed class AiInsightExpansionService : IAiInsightExpansionService
{
    private readonly ILlmClient _llmClient;
    private readonly ILogger<AiInsightExpansionService> _logger;

    public AiInsightExpansionService(
        ILlmClient llmClient,
        ILogger<AiInsightExpansionService> logger)
    {
        _llmClient = llmClient;
        _logger = logger;
    }

    public async Task<string?> GenerateInsightCommentAsync(
        MessageContext context,
        SocialMoveCandidate candidate,
        CancellationToken cancellationToken)
    {
        if (!ShouldUseInsightExpansion(context, candidate))
            return null;

        try
        {
            var prompt = BuildPrompt(context, candidate);
            var raw = await _llmClient.CompleteAsync(prompt, cancellationToken);

            var candidates = ParseCandidates(raw)
                .Where(c => IsValidInsightReply(c.Reply, context))
                .Select(c => ScoreCandidate(c, context))
                .OrderByDescending(c => c.TotalScore)
                .ToList();

            var best = candidates.FirstOrDefault();

            if (best is null)
            {
                _logger.LogInformation("AI insight expansion produced no valid candidates for move {Move}.", candidate.Move);
                return null;
            }

            if (best.TotalScore < 0.45)
            {
                _logger.LogInformation(
                    "AI insight expansion best candidate score {Score} did not meet threshold for move {Move}.",
                    best.TotalScore,
                    candidate.Move);
                return null;
            }

            _logger.LogInformation(
                "AI insight expansion selected {Angle} candidate with score {Score} for move {Move}.",
                best.Angle,
                best.TotalScore,
                candidate.Move);

            return best.Reply;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI insight expansion failed. Falling back to deterministic reply.");
            return null;
        }
    }

    private static bool ShouldUseInsightExpansion(
        MessageContext context,
        SocialMoveCandidate candidate)
    {
        var surface = context.Surface?.ToLowerInvariant() ?? "";
        var move = candidate.Move?.ToLowerInvariant() ?? "";
        var source = $"{context.SourceTitle} {context.SourceText}".ToLowerInvariant();

        if (surface != "feed_reply")
            return false;

        if (move is not ("add_insight" or "add_specific_insight" or "add_nuance" or "engage"))
            return false;

        return source.Contains("architecture") ||
               source.Contains("multi model") ||
               source.Contains("multimodel") ||
               source.Contains("provider") ||
               source.Contains("routing") ||
               source.Contains("ai") ||
               source.Contains("agent") ||
               source.Contains("resilience") ||
               source.Contains("system") ||
               source.Contains("workflow") ||
               source.Contains("scale") ||
               source.Contains("production");
    }

    private static string BuildPrompt(
        MessageContext context,
        SocialMoveCandidate candidate)
    {
        return $$"""
You are Sovereign, a social intelligence assistant for LinkedIn.

Generate 5 distinct candidate comments for a thoughtful LinkedIn reply.

Return ONLY valid JSON:
{
  "candidates": [
    {
      "angle": "tradeoff",
      "reply": "comment text",
      "confidence": 0.0
    }
  ]
}

Goal:
Each candidate must extend the author's idea with one practitioner-level insight.

Required angles:
1. "tradeoff" - hidden trade-off or failure mode
2. "implementation" - implementation nuance or operational constraint
3. "strategy" - strategic implication
4. "evaluation" - measurement/evaluation angle
5. "systems" - systems or architecture angle

Hard rules:
- Do NOT summarize the post.
- Do NOT restate the author's main point.
- Do NOT compliment without adding substance.
- Do NOT sound like an assistant, consultant, or corporate summary.
- Do NOT invent facts, statistics, events, names, or claims.
- Do NOT use hashtags.
- Do NOT mention Sovereign.
- Do NOT say: "great post", "well said", "thanks for sharing", "your experience underscores", "you nailed", "what stayed with me", "point around", "spot on".
- Keep each comment under 45 words.
- Each candidate must be meaningfully different from the others.
- Each candidate must add NEW value beyond the original post.

Few-shot examples:

Bad:
"Multi-model architecture is important because one provider can go down."
Why bad:
Repeats the author's point.

Good:
"Provider redundancy only works when prompt and evaluation layers are portable too -- otherwise failover preserves uptime but changes behavior."

Bad:
"Abstracting prompts and routing between providers can keep services running."
Why bad:
Paraphrases the post.

Good:
"Switching providers is only operationally safe when the evaluation layer can prove behavior stayed within bounds after the route changes."

Bad:
"AI agents will transform enterprise workflows."
Why bad:
Generic trend statement.

Good:
"The real constraint with agentic systems is rarely model quality -- it's observability and control once autonomous workflows hit production."

Post author:
{{context.SourceAuthor}}

Post title/profile:
{{context.SourceTitle}}

Post:
{{context.SourceText}}

Selected move:
{{candidate.Move}}
""";
    }

    private static bool IsValidInsightReply(string reply, MessageContext context)
    {
        var text = reply.Trim().ToLowerInvariant();

        if (text.Length < 25 || text.Length > 320)
            return false;

        var banned = new[]
        {
            "great post",
            "well said",
            "thanks for sharing",
            "your experience underscores",
            "you nailed",
            "what stayed with me",
            "point around",
            "spot on",
            "very insightful",
            "love this",
            "so true"
        };

        if (banned.Any(text.Contains))
            return false;

        if (ContainsUnsupportedNumber(reply, context))
            return false;

        if (IsTooDerivative(reply, context))
            return false;

        return true;
    }

    private static InsightCandidate ScoreCandidate(
        InsightCandidate candidate,
        MessageContext context)
    {
        candidate.NoveltyScore = ScoreNovelty(candidate.Reply, context);
        candidate.GroundingScore = ScoreGrounding(candidate.Reply, context);
        candidate.PractitionerToneScore = ScorePractitionerTone(candidate.Reply);
        candidate.DerivativePenalty = ScoreDerivativePenalty(candidate.Reply, context);
        candidate.GenericPenalty = ScoreGenericPenalty(candidate.Reply);
        candidate.RiskPenalty = ScoreRiskPenalty(candidate.Reply, context);

        candidate.TotalScore =
            (0.30 * candidate.NoveltyScore) +
            (0.20 * candidate.PractitionerToneScore) +
            (0.20 * candidate.GroundingScore) -
            (0.25 * candidate.DerivativePenalty) -
            (0.20 * candidate.GenericPenalty) -
            (0.15 * candidate.RiskPenalty);

        candidate.Reasons = BuildReasons(candidate);
        return candidate;
    }

    private static double ScoreNovelty(string reply, MessageContext context)
    {
        var text = reply.ToLowerInvariant();
        var source = (context.SourceText ?? string.Empty).ToLowerInvariant();

        var noveltyMarkers = new[]
        {
            "hidden", "trade-off", "tradeoff", "failure mode", "operational",
            "evaluation", "eval", "measurement", "observability", "governance",
            "behavior", "portability", "consistency", "rollback", "debugging",
            "monitoring", "coupling", "abstraction", "boundary", "drift"
        };

        var score = 0.0;

        score += Math.Min(0.45, noveltyMarkers.Count(text.Contains) * 0.10);

        var replyTerms = ImportantTerms(text);
        var sourceTerms = ImportantTerms(source);

        var newTerms = replyTerms.Count(t => !sourceTerms.Contains(t));
        score += Math.Min(0.35, newTerms * 0.04);

        if (text.Contains("not just") || text.Contains("rather than") || text.Contains("only when") || text.Contains("unless"))
            score += 0.15;

        if (text.Contains("otherwise") || text.Contains("where") || text.Contains("once"))
            score += 0.10;

        return Clamp01(score);
    }

    private static double ScoreGrounding(string reply, MessageContext context)
    {
        var source = (context.SourceText ?? string.Empty).ToLowerInvariant();
        var text = reply.ToLowerInvariant();

        var sourceTerms = ImportantTerms(source);
        var replyTerms = ImportantTerms(text);

        if (sourceTerms.Count == 0)
            return 0.5;

        var overlap = replyTerms.Count(t => sourceTerms.Contains(t));

        if (overlap == 0)
            return 0.15;

        if (overlap <= 2)
            return 0.75;

        if (overlap <= 5)
            return 1.0;

        return 0.65;
    }

    private static double ScorePractitionerTone(string reply)
    {
        var text = reply.ToLowerInvariant();

        var practitionerMarkers = new[]
        {
            "in production", "operational", "evaluation", "observability",
            "rollback", "guardrails", "workflow", "debugging", "routing",
            "consistency", "governance", "coupling", "portability",
            "behavior", "failure mode", "constraints"
        };

        var score = Math.Min(0.75, practitionerMarkers.Count(text.Contains) * 0.12);

        if (text.Contains("teams") || text.Contains("systems") || text.Contains("workflows"))
            score += 0.10;

        if (text.Contains("usually") || text.Contains("often") || text.Contains("tends to"))
            score += 0.10;

        return Clamp01(score);
    }

    private static double ScoreDerivativePenalty(string reply, MessageContext context)
    {
        var text = reply.ToLowerInvariant();
        var source = (context.SourceText ?? string.Empty).ToLowerInvariant();

        var sourceTerms = ImportantTerms(source);
        var replyTerms = ImportantTerms(text);

        if (replyTerms.Count == 0)
            return 1.0;

        var overlapRatio = replyTerms.Count(t => sourceTerms.Contains(t)) / (double)replyTerms.Count;

        var penalty = 0.0;

        if (overlapRatio > 0.55)
            penalty += 0.45;
        else if (overlapRatio > 0.40)
            penalty += 0.25;

        var repeatedPhrases = new[]
        {
            "interchangeable endpoints",
            "abstracting prompts",
            "routing between providers",
            "single provider",
            "single point of failure",
            "multi model architecture",
            "provider can go dark",
            "models as interchangeable",
            "system around it is the product"
        };

        penalty += repeatedPhrases.Count(p => source.Contains(p) && text.Contains(p)) * 0.18;

        if (text.StartsWith("this is") || text.StartsWith("your") || text.StartsWith("you"))
            penalty += 0.10;

        return Clamp01(penalty);
    }

    private static double ScoreGenericPenalty(string reply)
    {
        var text = reply.ToLowerInvariant();

        var banned = new[]
        {
            "great post", "well said", "thanks for sharing", "spot on",
            "you nailed", "your experience underscores", "what stayed with me",
            "point around", "very insightful", "love this", "so true",
            "key resilience tactic", "pragmatic way"
        };

        var penalty = banned.Count(text.Contains) * 0.25;

        if (text.Length < 35)
            penalty += 0.15;

        if (!text.Contains("—") && !text.Contains(";") && !text.Contains("because") && !text.Contains("when") && !text.Contains("where"))
            penalty += 0.10;

        return Clamp01(penalty);
    }

    private static double ScoreRiskPenalty(string reply, MessageContext context)
    {
        var penalty = 0.0;

        if (ContainsUnsupportedNumber(reply, context))
            penalty += 0.60;

        var text = reply.ToLowerInvariant();

        var risky = new[]
        {
            "always", "never", "guarantee", "proves", "definitely",
            "everyone", "no one", "must be"
        };

        penalty += risky.Count(text.Contains) * 0.08;

        return Clamp01(penalty);
    }

    private static bool IsTooDerivative(string reply, MessageContext context)
    {
        var source = (context.SourceText ?? string.Empty).ToLowerInvariant();
        var text = reply.ToLowerInvariant();

        var sourceTerms = ImportantTerms(source);
        var replyTerms = ImportantTerms(text);

        if (replyTerms.Count == 0)
            return true;

        var overlap = replyTerms.Count(t => sourceTerms.Contains(t));
        return (double)overlap / replyTerms.Count > 0.55;
    }

    private static bool ContainsUnsupportedNumber(string reply, MessageContext context)
    {
        var source = $"{context.SourceText} {context.SourceTitle}";
        var numbers = Regex
            .Matches(reply, @"\b\d+(\.\d+)?%?\b")
            .Select(m => m.Value)
            .Distinct()
            .ToArray();

        return numbers.Any(n => !source.Contains(n, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<InsightCandidate> ParseCandidates(string raw)
    {
        var json = ExtractJsonObject(raw);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("candidates", out var candidatesElement) ||
                candidatesElement.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<InsightCandidate>();
            }

            return candidatesElement.EnumerateArray()
                .Select(item => new InsightCandidate
                {
                    Angle = TryGetString(item, "angle") ?? string.Empty,
                    Reply = TryGetString(item, "reply") ?? string.Empty,
                    Confidence = TryGetDouble(item, "confidence") ?? 0.75
                })
                .Where(c => !string.IsNullOrWhiteSpace(c.Reply))
                .ToArray();
        }
        catch
        {
            return Array.Empty<InsightCandidate>();
        }
    }

    private static string ExtractJsonObject(string text)
    {
        var trimmed = text.Trim();
        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');

        if (start >= 0 && end > start)
            return trimmed.Substring(start, end - start + 1);

        return trimmed;
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) &&
               property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
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

    private static HashSet<string> ImportantTerms(string text)
    {
        var stop = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the","a","an","and","or","but","if","then","than","that","this",
            "those","these","to","for","from","of","in","on","at","with","is",
            "are","was","were","be","been","being","as","by","it","its","into",
            "about","your","you","their","they","them","we","our","i","me","my",
            "not","just","more","most","very","really","post","model","models",
            "ai"
        };

        return Regex
            .Matches(text.ToLowerInvariant(), @"[a-z0-9][a-z0-9\-/+]{2,}")
            .Select(m => m.Value)
            .Where(t => t.Length >= 4)
            .Where(t => !stop.Contains(t))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static double Clamp01(double value)
    {
        return Math.Clamp(value, 0.0, 1.0);
    }

    private static string[] BuildReasons(InsightCandidate candidate)
    {
        var reasons = new List<string>();

        if (candidate.NoveltyScore >= 0.6)
            reasons.Add("high novelty");

        if (candidate.GroundingScore >= 0.75)
            reasons.Add("grounded in source terms");

        if (candidate.PractitionerToneScore >= 0.5)
            reasons.Add("strong practitioner tone");

        if (candidate.DerivativePenalty >= 0.4)
            reasons.Add("derivative risk");

        if (candidate.GenericPenalty >= 0.25)
            reasons.Add("generic phrasing risk");

        if (candidate.RiskPenalty >= 0.25)
            reasons.Add("assertion risk");

        return reasons.ToArray();
    }
}
