using System.Text.RegularExpressions;
using Sovereign.Domain.Models;
using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Services;

public sealed class CandidateScoringEngine : ICandidateScoringEngine
{
    private static readonly string[] GenericPhrases =
    [
        "great post",
        "thanks for sharing",
        "well said",
        "amazing insight",
        "love this perspective",
        "completely agree",
        "this is so true",
        "very insightful",
        "great insights",
        "interesting perspective"
    ];

    public IReadOnlyList<CandidateScore> Score(
        IReadOnlyList<SocialMoveCandidate> candidates,
        SocialSituation situation,
        MessageContext context,
        RelationshipAnalysis relationshipAnalysis)
    {
        return candidates.Select(candidate => ScoreCandidate(candidate, situation, context, relationshipAnalysis)).ToArray();
    }

    private static CandidateScore ScoreCandidate(
        SocialMoveCandidate candidate,
        SocialSituation situation,
        MessageContext context,
        RelationshipAnalysis relationshipAnalysis)
    {
        var reply = candidate.Reply ?? string.Empty;
        var move = Normalize(candidate.Move);
        if (!PassQualityGates(candidate, situation, context))
        {
            return new CandidateScore
            {
                Candidate = candidate,
                ComputedTotal = 0.0,
                DisqualifiedReason = GetDisqualReason(candidate, situation, context),
                GenericPenalty = ContainsGenericPhrase(reply) ? 0.30 : 0.0
            };
        }

        var relevance = ScoreRelevance(context, reply);
        var specificity = ScoreSpecificity(context, reply);
        var socialFit = ScoreSocialFit(move, context);
        var relationshipFit = ScoreRelationshipFit(context, relationshipAnalysis);
        var brevity = ScoreBrevity(reply, context);
        var tone = ScoreTone(reply, move, context);
        var ctaQuality = IsCtaSituation(situation, context) && LooksLikeDirectAnswer(reply) ? 1.0 : 0.4;
        var insightDepth = HasInsightShape(reply) ? 1.0 : 0.35;
        var genericPenalty = ComputeGenericPenalty(reply);
        var familyBoost = GetExpectedFamilies(Normalize(situation.Type)).Contains(move, StringComparer.OrdinalIgnoreCase) ? 0.22 : 0.0;

        var score = new CandidateScore
        {
            Candidate = candidate,
            Relevance = relevance,
            Specificity = specificity,
            SocialFit = socialFit,
            RelationshipFit = relationshipFit,
            Brevity = brevity,
            Tone = tone,
            CTAResponseQuality = ctaQuality,
            InsightDepth = insightDepth,
            GenericPenalty = genericPenalty,
            GenericPraisePenalty = genericPenalty,
            FamilyMatchBoost = familyBoost,
            RiskAdjustedValue = move == "no_reply" ? 0.20 : 0.75,
            TimingFit = 0.70,
            PositioningStrength = specificity,
            CtaParticipationPenalty = IsCtaSituation(situation, context) && !LooksLikeDirectAnswer(reply) ? 0.20 : 0.0,
            ParticipationWithoutPositionPenalty = IsCtaSituation(situation, context) && !HasInsightShape(reply) ? 0.20 : 0.0
        };

        score.ComputedTotal =
            (0.28 * relevance) +
            (0.18 * specificity) +
            (0.14 * socialFit) +
            (0.12 * relationshipFit) +
            (0.08 * brevity) +
            (0.08 * tone) +
            (0.06 * ctaQuality) +
            (0.06 * insightDepth) +
            familyBoost -
            genericPenalty;

        ApplyHardCapsAndFloors(score, context);
        return score;
    }

    private static void ApplyHardCapsAndFloors(CandidateScore score, MessageContext context)
    {
        var move = Normalize(score.Candidate.Move);
        var mustReply = MustReplyForSurface(context);
        var hasDraft = !string.IsNullOrWhiteSpace(context.Message);
        var hasSourceText = !string.IsNullOrWhiteSpace(context.SourceText);

        if (move == "no_reply" && mustReply)
        {
            score.NoReplyPenalty = Math.Max(score.NoReplyPenalty, score.ComputedTotal - 0.05);
            score.ComputedTotal = Math.Min(score.ComputedTotal, 0.05);
        }

        if (hasDraft && move is "rewrite_user_intent" or "draft_post" or "helpful_reply" or "respond_helpfully")
        {
            score.ComputedTotal = Math.Max(score.ComputedTotal, 0.62);
        }

        if (string.Equals(context.Surface, "feed_reply", StringComparison.OrdinalIgnoreCase) &&
            hasSourceText &&
            IsSpecificContextualReply(score.Candidate.Reply, context))
        {
            score.ComputedTotal = Math.Max(score.ComputedTotal, 0.60);
        }
    }

    private static bool PassQualityGates(SocialMoveCandidate candidate, SocialSituation situation, MessageContext context)
    {
        var move = Normalize(candidate.Move);
        var reply = (candidate.Reply ?? string.Empty).Trim();
        var situationType = Normalize(situation.Type);

        if (move == "no_reply")
        {
            return !MustReplyForSurface(context) &&
                   situationType is "defer_no_reply" or "controversial_no_reply" or "low_signal" or "sensitive" or "controversial";
        }

        if (string.IsNullOrWhiteSpace(reply))
            return false;

        if (reply.Length is < 8 or > 600)
            return false;

        if (string.Equals(context.Surface, "start_post", StringComparison.OrdinalIgnoreCase))
            return move is "draft_post" or "rewrite_user_intent" or "compose_post";

        if (!IsChatSurface(context) && !HasMinimumAnchorOverlap(reply, context, 1))
            return false;

        if (!IsChatSurface(context) && ContainsGenericPhrase(reply) && !HasMinimumAnchorOverlap(reply, context, 1))
            return false;

        return true;
    }

    private static string GetDisqualReason(SocialMoveCandidate candidate, SocialSituation situation, MessageContext context)
    {
        var move = Normalize(candidate.Move);
        var reply = (candidate.Reply ?? string.Empty).Trim();
        var situationType = Normalize(situation.Type);

        if (move == "no_reply" && MustReplyForSurface(context))
            return "no_reply not allowed on required-reply surface";

        if (move == "no_reply" &&
            situationType is not ("defer_no_reply" or "controversial_no_reply" or "low_signal" or "sensitive" or "controversial"))
            return "no_reply not allowed for situation";

        if (string.IsNullOrWhiteSpace(reply))
            return "reply is empty";

        if (reply.Length is < 8 or > 600)
            return "reply length out of range";

        if (!IsChatSurface(context) && !HasMinimumAnchorOverlap(reply, context, 1))
            return "reply not anchored to context";

        return "failed quality gate";
    }

    private static string[] GetExpectedFamilies(string situationType)
    {
        return situationType switch
        {
            "achievement_share" => ["praise", "congratulate", "congratulate_encourage"],
            "personal_update" => ["congratulate", "congratulate_encourage", "encourage"],
            "industry_news" => ["add_insight", "add_specific_insight", "ask_relevant_question"],
            "group_announcement" => ["acknowledge", "appreciate"],
            "holiday_greeting" => ["respond", "appreciate"],
            "relationship_preservation" => ["engage", "appreciate", "light_touch"],
            "job_search" => ["encourage", "congratulate", "offer_support"],
            "cta_engagement" => ["answer_supportively", "add_specific_insight", "add_insight"],
            "cta_or_question" => ["answer_supportively", "add_specific_insight", "ask_relevant_question"],
            "question" => ["answer_supportively", "ask_relevant_question"],
            "rewrite_feed_reply" => ["rewrite_user_intent", "light_touch", "add_specific_insight"],
            "rewrite_direct_message" => ["rewrite_user_intent", "respond_helpfully"],
            "direct_message" => ["respond_helpfully", "rewrite_user_intent"],
            "compose_post" => ["draft_post", "rewrite_user_intent"],
            "defer_no_reply" => ["no_reply"],
            "controversial_no_reply" => ["no_reply"],
            "low_signal" => ["no_reply"],
            "sensitive" => ["no_reply"],
            "controversial" => ["no_reply"],
            "reflection" => ["engage", "add_insight", "light_touch"],
            "greeting" => ["respond", "appreciate"],
            "opinion" => ["add_nuance", "add_insight", "ask_relevant_question"],
            "educational" => ["add_insight", "add_specific_insight", "ask_relevant_question"],
            _ => ["engage", "appreciate", "add_insight"]
        };
    }

    private static double ScoreRelevance(MessageContext context, string reply)
    {
        if (HasMinimumAnchorOverlap(reply, context, 2))
            return 1.0;
        if (HasMinimumAnchorOverlap(reply, context, 1))
            return 0.82;
        return IsChatSurface(context) ? 0.70 : 0.30;
    }

    private static double ScoreSpecificity(MessageContext context, string reply)
    {
        if (HasMinimumAnchorOverlap(reply, context, 2))
            return 1.0;
        if (reply.Length >= 80)
            return 0.80;
        if (HasMinimumAnchorOverlap(reply, context, 1))
            return 0.72;
        return 0.45;
    }

    private static double ScoreSocialFit(string move, MessageContext context)
    {
        if (IsChatSurface(context))
            return move is "rewrite_user_intent" or "respond_helpfully" or "respond" ? 1.0 : 0.70;

        if (string.Equals(context.Surface, "start_post", StringComparison.OrdinalIgnoreCase))
            return move is "draft_post" or "rewrite_user_intent" ? 1.0 : 0.65;

        return move is "add_insight" or "add_specific_insight" or "acknowledge" or "appreciate" or "praise" or "congratulate" or "engage"
            ? 1.0
            : 0.70;
    }

    private static double ScoreRelationshipFit(MessageContext context, RelationshipAnalysis relationshipAnalysis)
    {
        var baseScore = context.TotalInteractions > 0 ? 0.75 : 0.60;
        if (relationshipAnalysis.MomentumScore > 0.6)
            baseScore += 0.10;
        return Math.Min(1.0, baseScore);
    }

    private static double ScoreBrevity(string reply, MessageContext context)
    {
        if (string.Equals(context.Surface, "start_post", StringComparison.OrdinalIgnoreCase))
        {
            return reply.Length is >= 120 and <= 600 ? 1.0 : 0.45;
        }

        return reply.Length switch
        {
            >= 8 and <= 220 => 1.0,
            <= 320 => 0.80,
            <= 480 => 0.60,
            _ => 0.35
        };
    }

    private static double ScoreTone(string reply, string move, MessageContext context)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return 0.0;

        if (IsChatSurface(context) && reply.Contains("linkedin", StringComparison.OrdinalIgnoreCase))
            return 0.35;

        if (move == "answer_supportively" && LooksLikeDirectAnswer(reply))
            return 1.0;

        return 0.82;
    }

    private static double ComputeGenericPenalty(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return 0.0;

        var penalty = GenericPhrases.Any(phrase => reply.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            ? 0.30
            : 0.0;

        if (reply.Trim().Length < 20)
            penalty += 0.05;

        return Math.Min(0.60, penalty);
    }

    private static bool ContainsGenericPhrase(string reply)
    {
        return GenericPhrases.Any(phrase => reply.Contains(phrase, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasMinimumAnchorOverlap(string reply, MessageContext context, int requiredOverlap)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return false;

        var sourceTokens = ExtractContextTokens(context);
        if (sourceTokens.Count == 0)
            return IsChatSurface(context);

        var replyTokens = Regex.Matches(reply.ToLowerInvariant(), @"[a-z0-9][a-z0-9'\-/+]{3,}")
            .Select(match => match.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return replyTokens.Count(token => sourceTokens.Contains(token)) >= requiredOverlap;
    }

    private static HashSet<string> ExtractContextTokens(MessageContext context)
    {
        var source = string.Join(" ",
            context.SourceTitle ?? string.Empty,
            context.SourceText ?? string.Empty,
            context.SourceAuthor ?? string.Empty,
            context.ParentContextText ?? string.Empty,
            context.Message ?? string.Empty).ToLowerInvariant();

        return Regex.Matches(source, @"[a-z0-9][a-z0-9'\-/+]{3,}")
            .Select(match => match.Value)
            .Where(token => token.Length >= 4)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsSpecificContextualReply(string? reply, MessageContext context)
    {
        return !string.IsNullOrWhiteSpace(reply) &&
               !ContainsGenericPhrase(reply) &&
               HasMinimumAnchorOverlap(reply, context, 1) &&
               reply.Trim().Length >= 24;
    }

    private static bool LooksLikeDirectAnswer(string reply)
    {
        var normalized = (reply ?? string.Empty).Trim().ToLowerInvariant();
        return normalized.StartsWith("my take:") ||
               normalized.Contains("start with") ||
               normalized.Contains("focus on") ||
               normalized.Contains("because") ||
               normalized.Contains("in practice");
    }

    private static bool HasInsightShape(string reply)
    {
        var normalized = (reply ?? string.Empty).ToLowerInvariant();
        return normalized.Contains("trade-off") ||
               normalized.Contains("tradeoffs") ||
               normalized.Contains("coordination") ||
               normalized.Contains("execution") ||
               normalized.Contains("operational") ||
               normalized.Contains("because") ||
               normalized.Contains("not just");
    }

    private static bool IsCtaSituation(SocialSituation situation, MessageContext context)
    {
        var situationType = Normalize(situation.Type);
        if (situationType is "cta_engagement" or "cta_or_question" or "question")
            return true;

        var source = string.Join(" ",
            context.SourceText ?? string.Empty,
            context.ParentContextText ?? string.Empty,
            context.NearbyContextText ?? string.Empty).ToLowerInvariant();

        return source.Contains("comment below") || source.Contains("share your") || source.Contains("what do you think");
    }

    private static bool IsChatSurface(MessageContext context)
    {
        return string.Equals(context.InteractionMode, "chat", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(context.Surface, "messaging_chat", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(context.Surface, "direct_message", StringComparison.OrdinalIgnoreCase);
    }

    private static bool MustReplyForSurface(MessageContext context)
    {
        var surface = Normalize(context.Surface);
        var hasMessage = !string.IsNullOrWhiteSpace(context.Message);
        var hasSourceText = !string.IsNullOrWhiteSpace(context.SourceText);
        var allowNoReply = context.InteractionMetadata is not null &&
                           context.InteractionMetadata.TryGetValue("allow_no_reply", out var raw) &&
                           bool.TryParse(raw, out var parsed) &&
                           parsed;

        return surface switch
        {
            "feed_reply" => hasMessage && hasSourceText,
            "start_post" => hasMessage,
            "messaging_chat" => hasMessage && !allowNoReply,
            _ => false
        };
    }

    private static string Normalize(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant();
}
