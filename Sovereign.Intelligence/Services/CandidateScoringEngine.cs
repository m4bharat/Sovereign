using Sovereign.Domain.Models;
using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sovereign.Intelligence.Services;

public sealed class CandidateScoringEngine : ICandidateScoringEngine
{
    private static readonly string[] GenericPhrases =
    {
        "great post",
        "well said",
        "thanks for sharing",
        "great perspective",
        "very insightful",
        "important reminder",
        "so true",
        "love this",
        "nice breakdown",
        "clear breakdown"
    };

    public IReadOnlyList<CandidateScore> Score(
        IReadOnlyList<SocialMoveCandidate> candidates,
        SocialSituation situation,
        MessageContext context,
        RelationshipAnalysis relationshipAnalysis)
    {
        return candidates.Select(candidate =>
        {
            if (!PassQualityGates(candidate, situation, context))
            {
                return new CandidateScore
                {
                    Candidate = candidate,
                    ComputedTotal = 0.0,
                    DisqualifiedReason = GetDisqualReason(candidate, situation, context)
                };
            }

            var reply = candidate.Reply ?? string.Empty;
            var score = new CandidateScore
            {
                Candidate = candidate,
                Relevance = ScoreRelevance(context, reply),
                Specificity = ScoreSpecificity(context, reply),
                GenericPenalty = ComputeGenericPenalty(reply, context),
                CTAResponseQuality = IsCtaSituation((situation.Type ?? string.Empty).Trim().ToLowerInvariant(), context) && LooksLikeDirectAnswer(reply) ? 1.0 : 0.0,
                InsightDepth = HasInsightShape(reply) ? 1.0 : 0.0,
                SocialFit = ScoreSocialFit(candidate, context),
                RelationshipFit = ScoreRelationshipFit(context, relationshipAnalysis),
                Brevity = ScoreBrevity(reply),
                Tone = ScoreTone(reply, candidate),
                RiskAdjustedValue = 0.5,
                TimingFit = 0.5
            };

            score.ComputedTotal =
                (0.28 * score.Relevance) +
                (0.18 * score.Specificity) +
                (0.14 * score.SocialFit) +
                (0.10 * score.RelationshipFit) +
                (0.08 * score.Brevity) +
                (0.08 * score.Tone) +
                (0.08 * score.CTAResponseQuality) +
                (0.06 * score.InsightDepth) -
                score.GenericPenalty;

            var expectedFamilies = GetExpectedFamilies((situation.Type ?? string.Empty).ToLowerInvariant());
            score.FamilyMatchBoost = expectedFamilies.Contains(candidate.Move, StringComparer.OrdinalIgnoreCase) ? 0.30 : 0.0;
            score.ComputedTotal += score.FamilyMatchBoost;

            return score;
        }).ToList();
    }

    private static bool PassQualityGates(SocialMoveCandidate candidate, SocialSituation situation, MessageContext context)
    {
        var reply = (candidate.Reply ?? string.Empty).Trim();
        var move = (candidate.Move ?? string.Empty).Trim().ToLowerInvariant();
        var situationType = (situation.Type ?? string.Empty).Trim().ToLowerInvariant();

        if (string.Equals(situation.Type, "compose_post", StringComparison.OrdinalIgnoreCase))
        {
            return candidate.Move is "draft_post" or "rewrite_user_intent";
        }

        if (string.IsNullOrWhiteSpace(reply) && move != "no_reply")
            return false;

        if (move == "no_reply")
        {
            return situationType is "defer_no_reply" or "controversial_no_reply" or "low_signal" or "sensitive" or "controversial";
        }

        var allowedFamilies = GetExpectedFamilies(situationType);
        if (allowedFamilies.Length > 0 && !allowedFamilies.Contains(move, StringComparer.OrdinalIgnoreCase))
            return false;

        if (!IsChatSurface(context) && !HasMinimumAnchorOverlap(reply, context, requiredOverlap: 1))
            return false;

        if (!IsChatSurface(context) && IsGenericReply(reply))
            return false;

        if (IsCtaSituation(situationType, context) && !LooksLikeDirectAnswer(reply))
            return false;

        if ((move == "praise" || move == "congratulate" || move == "congratulate_encourage") &&
            !HasMilestoneOrResultLanguage(reply, context))
            return false;

        if ((move == "add_insight" || move == "add_specific_insight" || move == "add_nuance") &&
            !HasInsightShape(reply))
            return false;

        if (move == "acknowledge" && reply.Length > 240)
            return false;

        return true;
    }

    private static string[] GetExpectedFamilies(string situationType)
    {
        return situationType switch
        {
            "achievement_share" => new[] { "praise", "congratulate" },
            "personal_update" => new[] { "congratulate", "congratulate_encourage", "encourage" },
            "industry_news" => new[] { "add_insight", "add_specific_insight", "ask_relevant_question" },
            "group_announcement" => new[] { "acknowledge", "appreciate" },
            "holiday_greeting" => new[] { "respond", "appreciate" },
            "relationship_preservation" => new[] { "engage", "appreciate", "light_touch" },
            "job_search" => new[] { "encourage", "congratulate", "offer_support" },
            "cta_engagement" => new[] { "answer_supportively", "add_specific_insight", "add_insight" },
            "cta_or_question" => new[] { "answer_supportively", "add_specific_insight", "ask_relevant_question" },
            "question" => new[] { "answer_supportively", "ask_relevant_question" },
            "rewrite_feed_reply" => new[] { "rewrite_user_intent", "light_touch", "add_specific_insight" },
            "rewrite_direct_message" => new[] { "rewrite_user_intent", "respond_helpfully" },
            "direct_message" => new[] { "respond_helpfully", "rewrite_user_intent", "acknowledge_and_continue" },
            "compose_post" => new[] { "draft_post", "rewrite_user_intent", "outline_post" },
            "defer_no_reply" => new[] { "no_reply" },
            "controversial_no_reply" => new[] { "no_reply" },
            "low_signal" => new[] { "no_reply" },
            "sensitive" => new[] { "no_reply" },
            "controversial" => new[] { "no_reply" },
            "milestone" => new[] { "praise", "congratulate", "congratulate_encourage" },
            "educational" => new[] { "add_insight", "add_specific_insight", "ask_relevant_question", "appreciate" },
            "opinion" => new[] { "add_nuance", "add_insight", "ask_relevant_question", "agree" },
            "greeting" => new[] { "respond", "appreciate" },
            "achievement" => new[] { "praise", "congratulate", "appreciate" },
            "news" => new[] { "add_insight", "ask_relevant_question", "appreciate" },
            "update" => new[] { "acknowledge", "appreciate" },
            _ => new[] { "engage", "appreciate", "light_touch", "add_insight" }
        };
    }

    private static bool IsGenericReply(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return true;

        var text = reply.Trim().ToLowerInvariant();

        if (GenericPhrases.Any(p => text.Contains(p)))
            return true;

        if (text.Length <= 20)
            return true;

        return false;
    }

    private static bool HasMinimumAnchorOverlap(string reply, MessageContext context, int requiredOverlap)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return false;

        var source = string.Join(" ",
            context.SourceTitle ?? string.Empty,
            context.SourceText ?? string.Empty,
            context.ParentContextText ?? string.Empty,
            context.NearbyContextText ?? string.Empty);

        var sourceTokens = Regex.Matches(source.ToLowerInvariant(), @"[a-z0-9][a-z0-9\-/+]{2,}")
            .Select(m => m.Value)
            .Where(t => t.Length >= 4)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (sourceTokens.Count == 0)
            return true;

        var replyTokens = Regex.Matches(reply.ToLowerInvariant(), @"[a-z0-9][a-z0-9\-/+]{2,}")
            .Select(m => m.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var overlap = replyTokens.Count(t => sourceTokens.Contains(t));
        return overlap >= requiredOverlap;
    }

    private static bool LooksLikeDirectAnswer(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return false;

        var text = reply.Trim().ToLowerInvariant();

        if (text.StartsWith("my take:") || text.StartsWith("my view:"))
            return true;

        if (text.Contains("start with") || text.Contains("focus on") || text.Contains("because"))
            return true;

        if (text.Contains("in practice") || text.Contains("the bottleneck") || text.Contains("the trade-off"))
            return true;

        return false;
    }

    private static bool HasMilestoneOrResultLanguage(string reply, MessageContext context)
    {
        var text = reply.ToLowerInvariant();
        var source = string.Join(" ",
            context.SourceTitle ?? string.Empty,
            context.SourceText ?? string.Empty).ToLowerInvariant();

        var markers = new[]
        {
            "milestone", "promotion", "award", "launch", "achievement",
            "result", "progress", "step", "won", "shipped", "released",
            "moved", "grew", "delivered", "earned"
        };

        return markers.Any(m => text.Contains(m) || source.Contains(m));
    }

    private static bool HasInsightShape(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return false;

        var text = reply.ToLowerInvariant();

        var insightMarkers = new[]
        {
            "trade-off", "second-order", "second order", "downstream",
            "constraint", "bottleneck", "coordination", "the real challenge",
            "what changes", "where it gets hard", "in practice", "the risk is",
            "operational layer", "portability", "governance", "most teams underestimate"
        };

        return insightMarkers.Any(m => text.Contains(m)) ||
               text.Contains("because") ||
               text.Contains("rather than") ||
               text.Contains("not just");
    }

    private static bool IsCtaSituation(string situationType, MessageContext context)
    {
        return situationType is "cta_engagement" or "cta_or_question" or "question"
            || IsCtaEngagementPost(context);
    }

    private static bool IsChatSurface(MessageContext context)
    {
        return string.Equals(context.InteractionMode, "chat", StringComparison.OrdinalIgnoreCase)
            || string.Equals(context.Surface, "messaging_chat", StringComparison.OrdinalIgnoreCase)
            || string.Equals(context.Surface, "direct_message", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetDisqualReason(SocialMoveCandidate candidate, SocialSituation situation, MessageContext context)
    {
        var reply = (candidate.Reply ?? string.Empty).Trim();
        var move = (candidate.Move ?? string.Empty).Trim().ToLowerInvariant();
        var situationType = (situation.Type ?? string.Empty).Trim().ToLowerInvariant();

        if (move == "no_reply" &&
            situationType is not ("defer_no_reply" or "controversial_no_reply" or "low_signal" or "sensitive" or "controversial"))
            return "no_reply not allowed for this situation";

        var allowedFamilies = GetExpectedFamilies(situationType);
        if (allowedFamilies.Length > 0 && !allowedFamilies.Contains(move, StringComparer.OrdinalIgnoreCase))
            return "move family mismatch";

        if (!IsChatSurface(context) && !HasMinimumAnchorOverlap(reply, context, 1))
            return "reply not anchored to source";

        if (!IsChatSurface(context) && IsGenericReply(reply))
            return "generic public reply";

        if (IsCtaSituation(situationType, context) && !LooksLikeDirectAnswer(reply))
            return "cta reply does not answer directly";

        if ((move == "praise" || move == "congratulate" || move == "congratulate_encourage") &&
            !HasMilestoneOrResultLanguage(reply, context))
            return "praise/congratulate reply lacks milestone or result language";

        if ((move == "add_insight" || move == "add_specific_insight" || move == "add_nuance") &&
            !HasInsightShape(reply))
            return "insight reply lacks insight shape";

        return "failed quality gate";
    }

    private static double ComputeGenericPenalty(string? reply, MessageContext context)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return 0.0;

        var text = reply.Trim().ToLowerInvariant();
        var penalty = 0.0;

        foreach (var phrase in GenericPhrases)
        {
            if (text.Contains(phrase))
                penalty += 0.18;
        }

        if (text.Length < 20)
            penalty += 0.05;

        if (!HasMinimumAnchorOverlap(text, context, 1))
            penalty += 0.12;

        return Math.Min(penalty, 0.60);
    }

    private static double ScoreRelevance(MessageContext context, string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return 0.0;

        return HasMinimumAnchorOverlap(reply, context, 1) ? 1.0 : 0.35;
    }

    private static double ScoreSpecificity(MessageContext context, string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return 0.0;

        if (HasMinimumAnchorOverlap(reply, context, 2))
            return 1.0;

        return reply.Length > 60 ? 0.75 : 0.45;
    }

    private static double ScoreSocialFit(SocialMoveCandidate candidate, MessageContext context)
    {
        var move = (candidate.Move ?? string.Empty).Trim().ToLowerInvariant();

        if (IsChatSurface(context))
        {
            return move is "respond_helpfully" or "rewrite_user_intent" or "respond" ? 1.0 : 0.6;
        }

        return move is "engage" or "acknowledge" or "add_insight" or "add_specific_insight" or "praise" or "congratulate"
            ? 1.0
            : 0.65;
    }

    private static double ScoreRelationshipFit(MessageContext context, RelationshipAnalysis relationshipAnalysis)
    {
        if (IsChatSurface(context))
            return 1.0;

        return context.TotalInteractions > 0 ? 0.8 : 0.6;
    }

    private static double ScoreBrevity(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return 0.0;

        return reply.Length switch
        {
            <= 220 => 1.0,
            <= 320 => 0.8,
            <= 480 => 0.55,
            _ => 0.35
        };
    }

    private static double ScoreTone(string reply, SocialMoveCandidate candidate)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return 0.0;

        var move = (candidate.Move ?? string.Empty).Trim().ToLowerInvariant();

        if (move == "ask_relevant_question" && reply.Contains('?'))
            return 1.0;

        if (move == "answer_supportively" && LooksLikeDirectAnswer(reply))
            return 1.0;

        return 0.8;
    }

    private static bool IsCtaEngagementPost(MessageContext context)
    {
        var source = string.Join(" ",
            context.SourceTitle ?? string.Empty,
            context.SourceText ?? string.Empty,
            context.ParentContextText ?? string.Empty,
            context.NearbyContextText ?? string.Empty)
            .ToLowerInvariant();

        var signals = new[]
        {
            "drop in the comments",
            "comment below",
            "let me know",
            "tell me",
            "where are you right now",
            "which skill",
            "what are you learning next",
            "what are you working on",
            "share in the comments",
            "comment your",
            "reply with",
            "what's your next step"
        };

        return signals.Any(signal => source.Contains(signal));
    }
}
