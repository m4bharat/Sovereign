using System.Text.RegularExpressions;
using Sovereign.Domain.Models;
using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Services;

public sealed class CandidateReplyGenerator : ICandidateReplyGenerator
{
    private static readonly string[] GenericReplies =
    [
        "great post",
        "thanks for sharing",
        "well said",
        "amazing insight",
        "love this perspective",
        "completely agree",
        "very insightful",
        "this is so true",
        "great insights",
        "interesting perspective"
    ];

    private static readonly string[] ForcedCtaEndings =
    [
        "what do you think?",
        "thoughts?",
        "agree?"
    ];

    private static readonly string[] FormalAiPhrases =
    [
        "i appreciate you sharing this valuable insight",
        "this resonates deeply",
        "in today's fast-paced world"
    ];

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "about", "after", "again", "also", "amid", "been", "being", "below", "between", "build",
        "came", "clear", "could", "does", "doing", "down", "each", "else", "even", "ever",
        "from", "have", "here", "into", "just", "keep", "made", "make", "many", "more",
        "most", "much", "only", "over", "part", "post", "really", "same", "share", "some",
        "still", "take", "than", "that", "their", "them", "then", "there", "these", "they",
        "this", "those", "through", "today", "very", "what", "when", "where", "which", "while",
        "with", "work", "would", "your"
    };

    public IReadOnlyList<SocialMoveCandidate> Generate(
        IReadOnlyList<SocialMoveCandidate> moveCandidates,
        MessageContext context)
    {
        var hasUserDraft = !string.IsNullOrWhiteSpace(context.Message);

        return moveCandidates
            .Select(candidate => GenerateCandidate(candidate, context, hasUserDraft))
            .ToArray();
    }

    private static SocialMoveCandidate GenerateCandidate(
        SocialMoveCandidate candidate,
        MessageContext context,
        bool hasUserDraft)
    {
        var move = NormalizeMove(candidate.Move);
        var reply = move switch
        {
            "no_reply" => string.Empty,
            "draft_post" or "outline_post" => GeneratePostReply(context),
            "rewrite_user_intent" => GenerateRewriteReply(context),
            "respond_helpfully" or "respond" => GenerateChatReply(context),
            "answer_supportively" or "answer_question" => GenerateAnswerReply(context),
            "praise" or "congratulate" or "congratulate_encourage" => GenerateMilestoneReply(context),
            "acknowledge" or "acknowledge_update" or "appreciate" => GenerateAnnouncementReply(context),
            "add_insight" or "add_specific_insight" or "add_nuance" or "agree" => GenerateInsightReply(context),
            "engage" or "light_touch" or "light_touch_question" or "ask_relevant_question" => GenerateContextualReply(context),
            "encourage" or "offer_support" => GenerateSupportReply(context),
            _ => GenerateFallbackReply(context)
        };

        reply = FinalizeReply(reply, context, move);
        if (!PassesReplyQuality(reply, context, move))
        {
            reply = FinalizeReply(BuildSafeFallbackReply(context, move), context, move);
        }

        return new SocialMoveCandidate
        {
            Move = candidate.Move,
            Rationale = candidate.Rationale,
            Reply = reply,
            ShortReply = BuildShortReply(move, reply, context),
            GenerationConfidence = EstimateConfidence(move, reply, hasUserDraft, context),
            RequiresPolish = reply.Length > 120,
            Alternatives = candidate.Alternatives,
            RelationshipEffect = candidate.RelationshipEffect,
            RiskScore = candidate.RiskScore,
            OpportunityScore = candidate.OpportunityScore,
            GenericPenalty = IsGenericReply(reply) ? 0.30 : 0.0,
            SituationType = candidate.SituationType,
            Tone = candidate.Tone
        };
    }

    private static string GeneratePostReply(MessageContext context)
    {
        var prompt = (context.Message ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return "AI becomes useful when it changes execution, not when it stays inside a demo.\n\nThe teams pulling ahead are the ones connecting AI to real workflows, clear ownership, and measurable outcomes.\n\nThat shift is what turns experimentation into leverage.";
        }

        var topic = ExtractTopic(prompt);
        var keywords = ExtractMeaningfulKeywords(prompt, limit: 3);
        var hook = keywords.Count > 0
            ? $"{Capitalize(keywords[0])} is where AI stops being a trend and starts becoming an operating decision."
            : $"{Capitalize(topic)} is where execution starts to matter more than excitement.";

        var bodyAnchor = keywords.Count > 1 ? keywords[1] : topic;
        var closingAnchor = keywords.Count > 2 ? keywords[2] : "execution";

        return $"{hook}\n\nWhat usually slows teams down is not intent. It is the gap between experiments and repeatable workflows. That is why {bodyAnchor} matters so much: it forces clarity on who owns the process, how success is measured, and what should improve over time.\n\nThe strongest teams will treat AI like a capability they can operationalize, not just a feature they can announce. That is where {closingAnchor} starts compounding.";
    }

    private static string GenerateRewriteReply(MessageContext context)
    {
        var draft = (context.Message ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(draft) || IsCommandOnlyMessage(draft))
        {
            return IsChatSurface(context)
                ? GenerateChatReply(context)
                : GenerateContextualReply(context);
        }

        if (IsChatSurface(context))
        {
            return RewriteStandaloneSentence(draft);
        }

        if (string.Equals(context.Surface, "start_post", StringComparison.OrdinalIgnoreCase))
        {
            return GeneratePostReply(context);
        }

        var rewritten = RewriteStandaloneSentence(draft);
        if (!HasContextAnchor(rewritten, context))
        {
            var anchor = ExtractPrimaryAnchor(context);
            if (!string.IsNullOrWhiteSpace(anchor))
            {
                rewritten = $"{TrimEndingPunctuation(rewritten)} The part about {anchor} comes through clearly.";
            }
        }

        return rewritten;
    }

    private static string GenerateChatReply(MessageContext context)
    {
        var draft = (context.Message ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(draft) && !IsCommandOnlyMessage(draft))
        {
            return RewriteStandaloneSentence(draft);
        }

        var source = BuildSourceCorpus(context);
        if (source.Contains("thank", StringComparison.OrdinalIgnoreCase))
            return "Thank you, I really appreciate it.";
        if (source.Contains("reconnect", StringComparison.OrdinalIgnoreCase))
            return "Would be good to reconnect. Happy to catch up and hear what you're building.";
        if (source.Contains("hiring", StringComparison.OrdinalIgnoreCase) || source.Contains("role", StringComparison.OrdinalIgnoreCase))
            return "Thanks for reaching out. This sounds interesting and I'd be happy to hear more.";
        if (source.Contains("birthday", StringComparison.OrdinalIgnoreCase))
            return "Thank you so much. I really appreciate it.";

        return "Thanks, I appreciate the note.";
    }

    private static string GenerateAnswerReply(MessageContext context)
    {
        var anchor = ExtractPrimaryAnchor(context);
        if (!string.IsNullOrWhiteSpace(anchor))
        {
            return $"My take: start with the part of {anchor} that creates the biggest practical bottleneck, then build from there.";
        }

        return "My take: start with the biggest practical bottleneck, then build from there.";
    }

    private static string GenerateMilestoneReply(MessageContext context)
    {
        var author = (context.SourceAuthor ?? string.Empty).Trim();
        var anchor = ExtractPrimaryAnchor(context);
        var prefix = string.IsNullOrWhiteSpace(author) ? "Congratulations" : $"Congratulations {author}";

        if (!string.IsNullOrWhiteSpace(anchor))
        {
            return $"{prefix}. The progress around {anchor} makes this milestone feel earned.";
        }

        return $"{prefix}. This milestone feels well earned.";
    }

    private static string GenerateAnnouncementReply(MessageContext context)
    {
        var anchor = ExtractPrimaryAnchor(context);
        if (!string.IsNullOrWhiteSpace(anchor))
        {
            return $"{Capitalize(anchor)} looks like the kind of update that will matter more once it is live in the real workflow.";
        }

        return "This update looks likely to matter once it is tested in the real workflow.";
    }

    private static string GenerateInsightReply(MessageContext context)
    {
        var anchor = ExtractPrimaryAnchor(context);
        if (!string.IsNullOrWhiteSpace(anchor))
        {
            return $"The constraint around {anchor} usually shows up once it has to hold up in real execution, not in the first draft of the idea.";
        }

        return "The real challenge usually shows up in execution, where coordination and trade-offs become visible.";
    }

    private static string GenerateContextualReply(MessageContext context)
    {
        var anchor = ExtractPrimaryAnchor(context);
        if (!string.IsNullOrWhiteSpace(anchor))
        {
            return $"The practical value of {anchor} usually becomes clearer once people have to work through the trade-offs around it.";
        }

        return "The practical value usually becomes clearer once the trade-offs have to be worked through.";
    }

    private static string GenerateSupportReply(MessageContext context)
    {
        var anchor = ExtractPrimaryAnchor(context);
        if (!string.IsNullOrWhiteSpace(anchor))
        {
            return $"You are moving in a strong direction. Staying close to {anchor} should create real momentum.";
        }

        return "You are moving in a strong direction. This should create real momentum.";
    }

    private static string GenerateFallbackReply(MessageContext context)
    {
        if (string.Equals(context.Surface, "start_post", StringComparison.OrdinalIgnoreCase))
            return GeneratePostReply(context);

        if (IsChatSurface(context))
            return GenerateChatReply(context);

        return GenerateContextualReply(context);
    }

    private static string BuildSafeFallbackReply(MessageContext context, string move)
    {
        if (string.Equals(context.Surface, "feed_reply", StringComparison.OrdinalIgnoreCase))
        {
            var anchor = ExtractPrimaryAnchor(context);
            if (!string.IsNullOrWhiteSpace(anchor))
            {
                return $"The part about {anchor} gets more interesting once it has to hold up in day-to-day execution.";
            }

            return "The practical implication gets more interesting once it has to hold up in day-to-day execution.";
        }

        if (IsChatSurface(context))
        {
            return GenerateChatReply(context);
        }

        if (string.Equals(context.Surface, "start_post", StringComparison.OrdinalIgnoreCase) || move == "draft_post")
        {
            return GeneratePostReply(context);
        }

        return "There is a practical angle here that becomes clearer once it reaches real use.";
    }

    private static string FinalizeReply(string reply, MessageContext context, string move)
    {
        if (string.Equals(move, "no_reply", StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        var sanitized = SanitizeReply(reply, context);
        if (!IsChatSurface(context) &&
            !string.Equals(context.Surface, "start_post", StringComparison.OrdinalIgnoreCase) &&
            !HasContextAnchor(sanitized, context))
        {
            var anchor = ExtractPrimaryAnchor(context);
            if (!string.IsNullOrWhiteSpace(anchor))
            {
                sanitized = $"{TrimEndingPunctuation(sanitized)} The part about {anchor} stands out.";
            }
        }

        return sanitized;
    }

    private static string SanitizeReply(string reply, MessageContext context)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return string.Empty;

        var sanitized = reply.Trim();
        sanitized = sanitized.Trim('"', '\'', '“', '”');
        sanitized = Regex.Replace(sanitized, @"[ \t]{2,}", " ");
        sanitized = Regex.Replace(sanitized, @"\s+\n", "\n");
        sanitized = Regex.Replace(sanitized, @"\n{3,}", "\n\n");

        if (!(context.Message ?? string.Empty).Contains('#'))
        {
            sanitized = Regex.Replace(sanitized, @"#\w+", string.Empty).Trim();
            sanitized = Regex.Replace(sanitized, @"[ \t]{2,}", " ");
            sanitized = Regex.Replace(sanitized, @"\n{3,}", "\n\n");
        }

        return sanitized.Trim();
    }

    private static bool IsGenericReply(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return true;

        var normalized = reply.Trim().ToLowerInvariant();
        return GenericReplies.Any(phrase => normalized.Contains(phrase)) || normalized.Length < 12;
    }

    private static bool HasContextAnchor(string reply, MessageContext context)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return false;

        var replyText = reply.ToLowerInvariant();
        return ExtractContextKeywords(context).Any(keyword => replyText.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static bool PassesReplyQuality(string reply, MessageContext context, string move)
    {
        if (string.Equals(move, "no_reply", StringComparison.OrdinalIgnoreCase))
            return string.IsNullOrWhiteSpace(reply);

        if (string.IsNullOrWhiteSpace(reply))
            return false;

        if (reply.Length is < 8 or > 600)
            return false;

        if (IsGenericReply(reply) && !HasContextAnchor(reply, context))
            return false;

        if (ForcedCtaEndings.Any(ending => reply.EndsWith(ending, StringComparison.OrdinalIgnoreCase)) &&
            !UserAskedForEngagement(context))
            return false;

        if (FormalAiPhrases.Any(phrase => reply.Contains(phrase, StringComparison.OrdinalIgnoreCase)))
            return false;

        return true;
    }

    private static bool UserAskedForEngagement(MessageContext context)
    {
        var combined = BuildSourceCorpus(context);
        return combined.Contains("what do you think", StringComparison.OrdinalIgnoreCase) ||
               combined.Contains("thoughts?", StringComparison.OrdinalIgnoreCase) ||
               combined.Contains("agree?", StringComparison.OrdinalIgnoreCase) ||
               combined.Contains("share your", StringComparison.OrdinalIgnoreCase) ||
               combined.Contains("comment below", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractPrimaryAnchor(MessageContext context)
    {
        return ExtractContextKeywords(context).FirstOrDefault() ?? string.Empty;
    }

    private static List<string> ExtractContextKeywords(MessageContext context)
    {
        var values = new[]
        {
            context.SourceText,
            context.SourceTitle,
            context.SourceAuthor,
            context.ParentContextText,
            context.Message
        };

        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .SelectMany(value => Regex.Matches(value!, @"[A-Za-z0-9][A-Za-z0-9'\-/+]{3,}")
                .Select(match => match.Value.ToLowerInvariant()))
            .Where(token => token.Length >= 4)
            .Where(token => !StopWords.Contains(token))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();
    }

    private static List<string> ExtractMeaningfulKeywords(string text, int limit)
    {
        return Regex.Matches(text, @"[A-Za-z0-9][A-Za-z0-9'\-/+]{3,}")
            .Select(match => match.Value.ToLowerInvariant())
            .Where(token => !StopWords.Contains(token))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();
    }

    private static string BuildSourceCorpus(MessageContext context)
    {
        return string.Join(" ",
            context.SourceText ?? string.Empty,
            context.SourceTitle ?? string.Empty,
            context.SourceAuthor ?? string.Empty,
            context.ParentContextText ?? string.Empty,
            context.NearbyContextText ?? string.Empty,
            context.Message ?? string.Empty);
    }

    private static string BuildShortReply(string move, string reply, MessageContext context)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return string.Empty;

        if (move == "draft_post")
        {
            return (context.Message ?? string.Empty).Trim();
        }

        var firstSentence = Regex.Split(reply, @"(?<=[.!?])\s+").FirstOrDefault() ?? reply;
        return firstSentence.Length <= 120 ? firstSentence : firstSentence[..120].Trim();
    }

    private static double EstimateConfidence(string move, string reply, bool hasUserDraft, MessageContext context)
    {
        if (move == "no_reply")
            return 0.50;

        if (string.IsNullOrWhiteSpace(reply))
            return 0.20;

        if (hasUserDraft && move is "rewrite_user_intent" or "draft_post")
            return 0.92;

        if (HasContextAnchor(reply, context))
            return 0.86;

        return 0.74;
    }

    private static bool IsChatSurface(MessageContext context)
    {
        return string.Equals(context.InteractionMode, "chat", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(context.Surface, "messaging_chat", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(context.Surface, "direct_message", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCommandOnlyMessage(string? message)
    {
        var normalized = (message ?? string.Empty).Trim().ToLowerInvariant();
        return normalized is "reply" or "write reply" or "suggest reply" or "comment" or "write comment" or "make a comment";
    }

    private static string NormalizeMove(string? move)
    {
        return (move ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static string RewriteStandaloneSentence(string text)
    {
        var cleaned = Regex.Replace((text ?? string.Empty).Trim(), @"\s+", " ");
        if (string.IsNullOrWhiteSpace(cleaned))
            return string.Empty;

        cleaned = cleaned.Replace("Im ", "I'm ", StringComparison.OrdinalIgnoreCase);
        cleaned = Capitalize(cleaned);

        if (!cleaned.EndsWith(".") && !cleaned.EndsWith("!") && !cleaned.EndsWith("?"))
            cleaned += ".";

        return cleaned;
    }

    private static string ExtractTopic(string text)
    {
        var cleaned = (text ?? string.Empty).Trim();
        var lower = cleaned.ToLowerInvariant();
        var prefixes = new[]
        {
            "write a linkedin post on ",
            "write a post on ",
            "draft a linkedin post on ",
            "draft a post on ",
            "create a post on ",
            "linkedin post on ",
            "post on "
        };

        foreach (var prefix in prefixes)
        {
            if (lower.StartsWith(prefix, StringComparison.Ordinal))
                return cleaned[prefix.Length..].Trim(' ', '.', ':');
        }

        return cleaned.Trim(' ', '.', ':');
    }

    private static string TrimEndingPunctuation(string text)
    {
        return (text ?? string.Empty).Trim().TrimEnd('.', '!', '?');
    }

    private static string Capitalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return text.Length == 1
            ? text.ToUpperInvariant()
            : char.ToUpperInvariant(text[0]) + text[1..];
    }
}
