using Sovereign.Domain.Models;
using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sovereign.Intelligence.Services;

public sealed class CandidateReplyGenerator : ICandidateReplyGenerator
{
    private static readonly string[] ForbiddenGenericPhrases =
    {
        "great post",
        "well said",
        "thanks for sharing",
        "great perspective",
        "very insightful",
        "so true",
        "important reminder",
        "nice breakdown",
        "clear breakdown",
        "real-world execution",
        "operationalize",
        "deployment speed",
        "at scale",
        "execution and scale"
    };

    public IReadOnlyList<SocialMoveCandidate> Generate(
        IReadOnlyList<SocialMoveCandidate> moveCandidates,
        MessageContext context)
    {
        var hasUserDraft = !string.IsNullOrWhiteSpace((context.Message ?? string.Empty).Trim());

        return moveCandidates
            .Select(candidate => GenerateCandidate(candidate, context, hasUserDraft))
            .ToArray();
    }

    private static SocialMoveCandidate GenerateCandidate(
        SocialMoveCandidate candidate,
        MessageContext context,
        bool hasUserDraft)
    {
        var move = (candidate.Move ?? string.Empty).Trim().ToLowerInvariant();

        var reply = move switch
        {
            "praise" => GeneratePraiseReply(context, candidate),
            "congratulate" => GenerateCongratulateReply(context, candidate),
            "congratulate_encourage" => GenerateCongratulateEncourageReply(context, candidate),
            "acknowledge" => GenerateAcknowledgeReply(context, candidate),
            "acknowledge_update" => GenerateAcknowledgeReply(context, candidate),
            "respond" => GenerateRespondReply(context, candidate),
            "engage" => GenerateEngageReply(context, candidate),
            "add_insight" => GenerateInsightReply(context, candidate),
            "add_specific_insight" => GenerateInsightReply(context, candidate),
            "add_nuance" => GenerateInsightReply(context, candidate),
            "encourage" => GenerateEncourageReply(context, candidate),
            "answer_supportively" => GenerateCtaAnswerReply(context, candidate),
            "answer_question" => GenerateCtaAnswerReply(context, candidate),
            "ask_relevant_question" => GenerateRelevantQuestionReply(context, candidate),
            "rewrite_user_intent" => RewriteUserDraft(context, candidate),
            "respond_helpfully" => GenerateHelpfulReply(context, candidate),
            "light_touch" => GenerateLightTouchReply(context, candidate),
            "light_touch_question" => GenerateRelevantQuestionReply(context, candidate),
            "no_reply" => string.Empty,
            "draft_post" => GenerateDraftPostReply(context, candidate),
            "outline_post" => GeneratePostOutline(context.Message ?? string.Empty),
            "appreciate" => GenerateAcknowledgeReply(context, candidate),
            "appreciate_journey" => GeneratePraiseReply(context, candidate),
            "express_interest" => RewriteStandaloneSentence(context.Message ?? string.Empty),
            "amplify_signal" => GenerateEngageReply(context, candidate),
            "offer_support" => GenerateHelpfulReply(context, candidate),
            "agree" => GenerateInsightReply(context, candidate),
            "defer" => string.Empty,
            "direct_message" => GenerateHelpfulReply(context, candidate),
            "ask_details" => GenerateRelevantQuestionReply(context, candidate),
            "engage_privately" => GenerateHelpfulReply(context, candidate),
            _ => GenerateFallbackReply(context, candidate)
        };

        reply = NormalizeReply(reply);

        if (move is not ("draft_post" or "outline_post"))
        {
            reply = EnforceAnchoring(reply, context, move);
            reply = FilterGenericReply(reply, context, move);
        }

        var shortReply = BuildShortReplyForMove(move, context);

        return new SocialMoveCandidate
        {
            Move = candidate.Move,
            Rationale = candidate.Rationale,
            Reply = reply,
            ShortReply = shortReply,
            GenerationConfidence = EstimateConfidence(move, reply, hasUserDraft, context.SourceText ?? string.Empty),
            RequiresPolish = ShouldPolish(reply),
            Alternatives = candidate.Alternatives,
            RelationshipEffect = candidate.RelationshipEffect,
            RiskScore = candidate.RiskScore,
            OpportunityScore = candidate.OpportunityScore,
            GenericPenalty = ContainsForbiddenGenericPhrase(reply) ? 0.25 : candidate.GenericPenalty,
            SituationType = candidate.SituationType,
            Tone = candidate.Tone
        };
    }

    private static string NormalizeReply(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        text = Regex.Replace(text.Trim(), @"\s+", " ");
        text = text.Replace(" ,", ",").Replace(" .", ".").Replace(" !", "!").Replace(" ?", "?");

        return text.Trim();
    }

    private static bool ContainsForbiddenGenericPhrase(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return ForbiddenGenericPhrases.Any(p =>
            text.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    private static string EnforceAnchoring(string reply, MessageContext context, string move)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return reply;

        if (IsChatSurface(context))
            return reply;

        if (HasSourceAnchor(reply, context))
            return reply;

        var anchor = ExtractBestAnchor(context);
        if (string.IsNullOrWhiteSpace(anchor))
            return reply;

        return move switch
        {
            "praise" => $"{reply} The {anchor} really stands out.",
            "congratulate" => $"{reply} Big moment around {anchor}.",
            "acknowledge" => $"{reply} The update on {anchor} landed clearly.",
            "engage" => $"{reply} The point on {anchor} is what stayed with me.",
            "add_insight" or "add_specific_insight" => $"{reply} That matters most around {anchor}.",
            "answer_supportively" => $"{reply} Especially in the context of {anchor}.",
            _ => $"{reply} Especially around {anchor}."
        };
    }

    private static bool HasSourceAnchor(string reply, MessageContext context)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return false;

        var sourceTokens = ExtractAnchorTokens(context);
        if (sourceTokens.Count == 0)
            return false;

        var lowerReply = reply.ToLowerInvariant();
        return sourceTokens.Any(t => lowerReply.Contains(t));
    }

    private static HashSet<string> ExtractAnchorTokens(MessageContext context)
    {
        var source = string.Join(" ",
            context.SourceTitle ?? string.Empty,
            context.SourceText ?? string.Empty,
            context.ParentContextText ?? string.Empty,
            context.NearbyContextText ?? string.Empty);

        var prioritizedPhrases = new[]
        {
            "multi model",
            "provider dependency",
            "routing",
            "architecture",
            "resilience",
            "orchestration"
        }.Where(phrase => source.Contains(phrase, StringComparison.OrdinalIgnoreCase))
         .Select(phrase => phrase.ToLowerInvariant());

        var hashtagTokens = Regex.Matches(source, @"#([A-Za-z][A-Za-z0-9]+)")
            .Select(m => m.Groups[1].Value.ToLowerInvariant());

        var companyTokens = Regex.Matches(source, @"\b[A-Z][A-Za-z0-9&.-]{2,}\b")
            .Select(m => m.Value.ToLowerInvariant());

        return prioritizedPhrases
            .Concat(hashtagTokens)
            .Concat(companyTokens)
            .Concat(Regex.Matches(source.ToLowerInvariant(), @"[a-z0-9][a-z0-9\-/+]{2,}")
            .Select(m => m.Value)
            .Where(t => t.Length >= 4)
            .Where(t => !IsWeakAnchorToken(t)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsWeakAnchorToken(string token)
    {
        return token is
            "this" or "that" or "with" or "from" or "your" or "about" or "have" or
            "more" or "than" or "into" or "they" or "them" or "their" or "there" or
            "would" or "could" or "should" or "really" or "very" or "just" or "post" or
            "associate" or "developer" or "java" or "spring" or "boot" or "rest" or
            "mysql" or "mongodb" or "ai" or "model" or "provider";
    }

    private static string ExtractBestAnchor(MessageContext context)
    {
        var tokens = ExtractAnchorTokens(context);
        return tokens.FirstOrDefault() ?? string.Empty;
    }

    private static bool IsChatSurface(MessageContext context)
    {
        return string.Equals(context.InteractionMode, "chat", StringComparison.OrdinalIgnoreCase)
            || string.Equals(context.Surface, "messaging_chat", StringComparison.OrdinalIgnoreCase)
            || string.Equals(context.Surface, "direct_message", StringComparison.OrdinalIgnoreCase);
    }

    private static string FilterGenericReply(string reply, MessageContext context, string move)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return reply;

        if (!ContainsForbiddenGenericPhrase(reply))
            return reply;

        var anchor = ExtractBestAnchor(context);
        if (string.IsNullOrWhiteSpace(anchor))
            anchor = "the core point";

        return move switch
        {
            "praise" => $"Impressive work here — the progress around {anchor} is easy to notice.",
            "congratulate" => $"Congratulations on this milestone. The work around {anchor} clearly moved forward.",
            "acknowledge" => $"Appreciate the update here. The signal around {anchor} came through clearly.",
            "engage" => $"What stood out to me was the point around {anchor}. That’s where the post gets interesting.",
            "add_insight" or "add_specific_insight" => $"The interesting part is what {anchor} changes downstream once it hits real usage.",
            "answer_supportively" => $"My take: start with the part of {anchor} that creates the biggest practical bottleneck, then build from there.",
            _ => $"The point around {anchor} is what makes this worth engaging with."
        };
    }

    private static string GeneratePraiseReply(MessageContext context, SocialMoveCandidate candidate)
    {
        var anchor = ExtractBestAnchor(context);
        var author = context.SourceAuthor?.Trim();

        if (!string.IsNullOrWhiteSpace(author) && !string.IsNullOrWhiteSpace(anchor))
            return $"{author}, this is strong work. The progress around {anchor} really stands out.";

        if (!string.IsNullOrWhiteSpace(anchor))
            return $"Strong work here. The result around {anchor} really stands out.";

        return "Strong work here. The result comes through clearly.";
    }

    private static string GenerateCongratulateReply(MessageContext context, SocialMoveCandidate candidate)
    {
        var anchor = ExtractBestAnchor(context);
        var author = context.SourceAuthor?.Trim();

        if (!string.IsNullOrWhiteSpace(author) && !string.IsNullOrWhiteSpace(anchor))
            return $"Congratulations {author} — big milestone, and the work around {anchor} makes that clear.";

        if (!string.IsNullOrWhiteSpace(anchor))
            return $"Congratulations on the milestone. The progress around {anchor} makes it feel earned.";

        return "Congratulations on the milestone — this feels well earned.";
    }

    private static string GenerateCongratulateEncourageReply(MessageContext context, SocialMoveCandidate candidate)
    {
        var anchor = ExtractBestAnchor(context);

        if (!string.IsNullOrWhiteSpace(anchor))
            return $"Congratulations on this step. Excited to see where you take {anchor} next.";

        return "Congratulations on this step. Excited to see where you take it next.";
    }

    private static string GenerateAcknowledgeReply(MessageContext context, SocialMoveCandidate candidate)
    {
        var anchor = ExtractBestAnchor(context);

        if (!string.IsNullOrWhiteSpace(anchor))
            return $"Appreciate the update here. The signal around {anchor} came through clearly.";

        return "Appreciate the update here.";
    }

    private static string GenerateRespondReply(MessageContext context, SocialMoveCandidate candidate)
    {
        var author = context.SourceAuthor?.Trim();

        if (!string.IsNullOrWhiteSpace(author))
            return $"Thank you, {author}. Wishing you the same.";

        return "Thank you — wishing you the same.";
    }

    private static string GenerateEngageReply(MessageContext context, SocialMoveCandidate candidate)
    {
        var anchor = ExtractBestAnchor(context);

        if (!string.IsNullOrWhiteSpace(anchor))
            return $"What stayed with me here was the point around {anchor}. That’s where this really connects.";

        return "What stayed with me here was the core point itself. That’s where this really connects.";
    }

    private static string GenerateInsightReply(MessageContext context, SocialMoveCandidate candidate)
    {
        var anchor = ExtractBestAnchor(context);
        var source = context.SourceText ?? string.Empty;

        if (ContainsAny(source,
            "multi model",
            "provider",
            "architecture",
            "routing",
            "dependency",
            "resilience"))
        {
            return "Strong point — provider redundancy only works when the orchestration layer is designed for portability from the start. Most teams underestimate how much provider-specific coupling sneaks into prompts and workflows.";
        }

        if (!string.IsNullOrWhiteSpace(anchor))
        {
            return $"The real complexity with {anchor} usually appears in the operational layer — that’s where portability and governance become much harder than model selection itself.";
        }

        return "The real complexity usually appears in the operational layer — that’s where execution becomes much harder than the idea itself.";
    }

    private static string GenerateEncourageReply(MessageContext context, SocialMoveCandidate candidate)
    {
        var anchor = ExtractBestAnchor(context);

        if (!string.IsNullOrWhiteSpace(anchor))
            return $"You’re moving in a good direction. Staying close to {anchor} should create real momentum.";

        return "You’re moving in a good direction. This should create real momentum.";
    }

    private static string GenerateCtaAnswerReply(MessageContext context, SocialMoveCandidate candidate)
    {
        var anchor = ExtractBestAnchor(context);

        if (!string.IsNullOrWhiteSpace(anchor))
            return $"My take: start with the part of {anchor} that creates the biggest practical bottleneck, then build from there.";

        return "My take: start with the part that creates the biggest practical bottleneck, then build from there.";
    }

    private static string GenerateRelevantQuestionReply(MessageContext context, SocialMoveCandidate candidate)
    {
        var anchor = ExtractBestAnchor(context);

        if (!string.IsNullOrWhiteSpace(anchor))
            return $"Curious where {anchor} becomes hardest in practice — adoption, coordination, or ongoing ownership?";

        return "Curious where this becomes hardest in practice — adoption, coordination, or ongoing ownership?";
    }

    private static string GenerateLightTouchReply(MessageContext context, SocialMoveCandidate candidate)
    {
        var anchor = ExtractBestAnchor(context);

        if (!string.IsNullOrWhiteSpace(anchor))
            return $"Good signal here on {anchor}.";

        return "Good signal here.";
    }

    private static string GenerateFallbackReply(MessageContext context, SocialMoveCandidate candidate)
    {
        var anchor = ExtractBestAnchor(context);

        if (!string.IsNullOrWhiteSpace(anchor))
            return $"The point around {anchor} is what makes this worth engaging with.";

        return "There’s a clear signal here worth engaging with.";
    }

    private static string GenerateHelpfulReply(MessageContext context, SocialMoveCandidate candidate)
    {
        var anchor = ExtractBestAnchor(context);
        var draft = (context.Message ?? string.Empty).Trim();

        if (IsCommandOnlyMessage(context.Message))
            return GenerateHelpfulReplyFromSource(context);

        if (!string.IsNullOrWhiteSpace(draft))
        {
            var rewritten = RewriteStandaloneSentence(draft);
            if (!string.IsNullOrWhiteSpace(anchor))
                return $"{rewritten} Especially in the context of {anchor}.";

            return rewritten;
        }

        if (!string.IsNullOrWhiteSpace(anchor))
            return $"Helpful direction here starts with the practical bottleneck around {anchor}.";

        return "Helpful direction here starts with the practical bottleneck.";
    }

    private static string RewriteUserDraft(MessageContext context, SocialMoveCandidate candidate)
    {
        if (IsCommandOnlyMessage(context.Message))
        {
            if (IsChatSurface(context))
                return GenerateHelpfulReplyFromSource(context);

            return GenerateFallbackReply(context, candidate);
        }

        var draft = (context.Message ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(draft))
            return string.Empty;

        var rewritten = RewriteStandaloneSentence(draft);

        if (!IsChatSurface(context) && !HasSourceAnchor(rewritten, context))
        {
            rewritten = EnforceAnchoring(rewritten, context, "rewrite_user_intent");
        }

        rewritten = NormalizeReply(rewritten);

        if (ContainsForbiddenGenericPhrase(rewritten))
        {
            rewritten = EnforceAnchoring(rewritten, context, "rewrite_user_intent");
            rewritten = FilterGenericReply(rewritten, context, "rewrite_user_intent");
        }

        return rewritten;
    }

    private static bool IsCommandOnlyMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        var text = message.Trim().ToLowerInvariant();

        return text is "reply" or "write reply" or "suggest reply" or "comment" or "write comment";
    }

    private static string GenerateHelpfulReplyFromSource(MessageContext context)
    {
        var source = string.Join(" ",
            context.SourceText ?? string.Empty,
            context.ParentContextText ?? string.Empty,
            context.NearbyContextText ?? string.Empty);

        if (source.Contains("happy belated birthday", StringComparison.OrdinalIgnoreCase))
            return "Thank you so much! Really appreciate your wishes.";

        if (source.Contains("happy birthday", StringComparison.OrdinalIgnoreCase))
            return "Thank you so much! Really appreciate it.";

        return "Thanks, I appreciate it.";
    }

    private static string BuildShortReplyForMove(string move, MessageContext context)
    {
        var anchor = ExtractBestAnchor(context);

        return move switch
        {
            "congratulate" => string.IsNullOrWhiteSpace(anchor)
                ? "Congratulations on the milestone."
                : $"Congratulations on {anchor}.",
            "ask_relevant_question" => string.IsNullOrWhiteSpace(anchor)
                ? "Where does this get hardest in practice?"
                : $"Where does {anchor} get hardest in practice?",
            "rewrite_user_intent" => RewriteStandaloneSentence(context.Message ?? string.Empty),
            "draft_post" => (context.Message ?? string.Empty).Trim(),
            "light_touch" => string.IsNullOrWhiteSpace(anchor)
                ? "Good signal here."
                : $"Good signal on {anchor}.",
            _ => string.IsNullOrWhiteSpace(anchor)
                ? "Clear signal here."
                : $"Clear signal on {anchor}."
        };
    }

    private static bool ShouldPolish(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return false;

        return reply.Length > 80;
    }

    private static double EstimateConfidence(string move, string reply, bool hasDraft, string sourceText)
    {
        if (string.Equals(move, "no_reply", StringComparison.OrdinalIgnoreCase))
            return 0.50;

        if (string.IsNullOrWhiteSpace(reply))
            return 0.25;

        if ((string.Equals(move, "rewrite_user_intent", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(move, "draft_post", StringComparison.OrdinalIgnoreCase)) && hasDraft)
            return 0.92;

        if (!string.IsNullOrWhiteSpace(sourceText) && reply.Length > 40)
            return 0.88;

        return 0.80;
    }

    private static string RewriteStandaloneSentence(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var cleaned = Regex.Replace(text.Trim(), "\\s+", " ");

        cleaned = cleaned.Replace("Im ", "I'm ", StringComparison.OrdinalIgnoreCase);
        cleaned = cleaned.Replace(" i ", " I ", StringComparison.Ordinal);
        cleaned = cleaned.Trim();

        if (cleaned.Length == 0)
            return string.Empty;

        cleaned = cleaned.Length == 1
            ? char.ToUpperInvariant(cleaned[0]).ToString()
            : char.ToUpperInvariant(cleaned[0]) + cleaned[1..];

        if (!cleaned.EndsWith(".") &&
            !cleaned.EndsWith("!") &&
            !cleaned.EndsWith("?"))
        {
            cleaned += ".";
        }

        return cleaned;
    }

    private static string GeneratePost(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
            return string.Empty;

        var normalized = RewriteStandaloneSentence(topic);

        return $"{normalized}\n\nWe are entering a phase where execution speed matters more than ideas alone. The real gap is no longer between those who understand the technology and those who do not — it is between those who can operationalize it at scale and those who cannot.\n\nThe next wave of advantage will come from deployment discipline, not just technical capability.";
    }

    private static string GenerateDraftPostReply(
        MessageContext context,
        SocialMoveCandidate candidate)
    {
        var prompt = context.Message?.Trim();

        if (string.IsNullOrWhiteSpace(prompt))
            return string.Empty;

        var topic = ExtractTopic(prompt);

        return
$@"AI is rapidly reshaping how teams operate — from automation and copilots to decision intelligence and workflow optimization.

The most important trend isn't just model capability anymore.

It's how effectively organizations integrate AI into real workflows and create measurable business value.

The winners in this next phase will be teams that move beyond experimentation and operationalize AI with clear use cases, strong data foundations, and tight feedback loops.

Curious how others are thinking about AI adoption in 2026.

#{ToHashtag(topic)} #ArtificialIntelligence #Innovation";
    }

    private static string GeneratePostOutline(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
            return string.Empty;

        var normalized = RewriteStandaloneSentence(topic);

        return $"{normalized}\n\n1. What is changing right now\n2. Why this matters beyond the surface trend\n3. Where the real competitive advantage will come from\n4. What this means going forward";
    }

    private static string ExtractTopic(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var cleaned = Regex.Replace(text.Trim(), @"\s+", " ");
        var lower = cleaned.ToLowerInvariant();

        var prefixes = new[]
        {
            "write a post on ",
            "write a linkedin post on ",
            "post on ",
            "linkedin post on ",
            "draft a post on ",
            "create a post on "
        };

        foreach (var prefix in prefixes)
        {
            if (lower.StartsWith(prefix))
            {
                return cleaned[prefix.Length..].Trim(' ', '.', '!', '?');
            }
        }

        return cleaned.Trim(' ', '.', '!', '?');
    }

    private static string ToHashtag(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "AI";

        var parts = Regex.Matches(text, @"[A-Za-z0-9]+")
            .Select(m => m.Value)
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToArray();

        if (parts.Length == 0)
            return "AI";

        return string.Concat(parts.Select(part =>
            part.Length == 1
                ? part.ToUpperInvariant()
                : char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()));
    }

    private static bool ContainsAny(string text, params string[] terms)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return terms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}
