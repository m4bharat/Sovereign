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

            var result = await _llmClient.CompleteDecisionV2Async(prompt, cancellationToken);

            var reply = result.Reply?.Trim();

            if (string.IsNullOrWhiteSpace(reply))
            {
                _logger.LogInformation("AI insight expansion returned an empty reply for move {Move}.", candidate.Move);
                return null;
            }

            if (!IsValidInsightReply(reply, context))
            {
                _logger.LogInformation("AI insight expansion reply was rejected by validation for move {Move}.", candidate.Move);
                return null;
            }

            _logger.LogInformation("AI insight expansion produced a validated reply for move {Move}.", candidate.Move);
            return reply;
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

Write ONE thoughtful LinkedIn comment.

Return ONLY valid JSON:
{
  "reply": "comment text",
  "confidence": 0.0,
  "alternatives": []
}

Core goal:
Extend the author's idea with one useful practitioner-level insight.

Hard rules:
- Do NOT summarize the post.
- Do NOT restate the author's main point.
- Do NOT compliment without adding substance.
- Do NOT sound like an assistant, consultant, or corporate summary.
- Do NOT invent facts, statistics, events, names, or claims.
- Do NOT use hashtags.
- Do NOT mention Sovereign.
- Do NOT say: "great post", "well said", "thanks for sharing", "your experience underscores", "you nailed", "what stayed with me", "point around".
- Keep the comment under 45 words.
- The comment must be safe to post directly on LinkedIn.

Required:
- Add ONE second-order implication, trade-off, implementation challenge, architectural nuance, or operational consideration that is not merely a paraphrase of the post.
- Sound like a practitioner/peer/operator with real experience.
- Be specific enough that it could not fit under any generic AI post.

Good vs bad examples:

Bad:
"Great point about using multiple providers for resilience."
Why bad:
Generic praise and restates the post.

Good:
"Provider redundancy only works when prompt and evaluation layers are portable too — otherwise failover preserves uptime but changes behavior."

Bad:
"Multi-model architecture is important because one provider can go down."
Why bad:
It repeats the author's point.

Good:
"The hidden coupling is usually behavioral, not infrastructural — prompts, guardrails, and evals often assume one model’s quirks long before teams notice."

Bad:
"AI agents will transform enterprise workflows."
Why bad:
Generic trend statement.

Good:
"The real constraint with agentic systems is rarely model quality — it’s observability and control once autonomous workflows hit production."

Bad:
"Abstracting prompts and routing between providers can keep services running."
Why bad:
It paraphrases the post.

Good:
"Switching providers is only operationally safe when the evaluation layer can prove behavior stayed within bounds after the route changes."

Now generate the comment.

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

    private static bool IsTooDerivative(string reply, MessageContext context)
    {
        var source = (context.SourceText ?? "").ToLowerInvariant();
        var text = reply.ToLowerInvariant();

        var sourceTokens = source.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Distinct()
            .ToHashSet();

        var replyTokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Distinct()
            .ToList();

        if (replyTokens.Count == 0)
            return true;

        var overlap = replyTokens.Count(t => sourceTokens.Contains(t));

        return (double)overlap / replyTokens.Count > 0.55;
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
}
