using Sovereign.Domain.Models;
using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Services;

public sealed class WinnerSelectionEngine : IWinnerSelectionEngine
{
    public WinnerSelectionResult SelectBest(
        IReadOnlyList<CandidateScore> scores,
        SocialSituation situation,
        MessageContext context)
    {
        var bestScore = SelectWinnerScore(scores, situation, context);
        var alternatives = BuildAlternatives(scores, bestScore, situation, context);

        return new WinnerSelectionResult
        {
            Winner = bestScore.Candidate,
            Alternatives = alternatives
        };
    }

    public WinnerSelectionResult SelectBest(
        IReadOnlyList<CandidateScore> scores,
        MessageContext context)
    {
        var situation = new SocialSituation
        {
            Type = context.SituationType ?? string.Empty
        };

        return SelectBest(scores, situation, context);
    }

    private static CandidateScore SelectWinnerScore(
        IReadOnlyList<CandidateScore> scores,
        SocialSituation situation,
        MessageContext context)
    {
        if (scores == null || scores.Count == 0)
        {
            return new CandidateScore
            {
                Candidate = new SocialMoveCandidate
                {
                    Move = "no_reply",
                    Reply = string.Empty,
                    Rationale = "No candidates were available."
                },
                ComputedTotal = 0.0,
                DisqualifiedReason = "no candidates"
            };
        }

        var situationType = (situation.Type ?? string.Empty).Trim().ToLowerInvariant();
        var preferredFamilies = GetPreferredMoveFamilies(situationType);
        var isExplicitNoReply = IsExplicitNoReplySituation(situationType);

        var qualified = scores
            .Where(IsQualified)
            .ToList();

        if (!isExplicitNoReply)
        {
            qualified = qualified
                .Where(s => !IsNoReply(s))
                .ToList();
        }

        if (qualified.Count == 0)
        {
            return BuildFallback(scores, isExplicitNoReply);
        }

        if (string.Equals(situation.Type, "compose_post", StringComparison.OrdinalIgnoreCase))
        {
            var draftPost = qualified
                .FirstOrDefault(s =>
                    string.Equals(
                        s.Candidate.Move,
                        "draft_post",
                        StringComparison.OrdinalIgnoreCase));

            if (draftPost != null)
                return draftPost;
        }

        var preferred = qualified
            .Where(s => IsPreferredFamily(s, preferredFamilies))
            .ToList();

        if (preferred.Count > 0)
        {
            return PickBest(preferred);
        }

        return PickBest(qualified);
    }

    private static IReadOnlyList<SocialMoveCandidate> BuildAlternatives(
        IReadOnlyList<CandidateScore> scores,
        CandidateScore winner,
        SocialSituation situation,
        MessageContext context)
    {
        if (scores == null || scores.Count == 0)
            return Array.Empty<SocialMoveCandidate>();

        var situationType = (situation.Type ?? string.Empty).Trim().ToLowerInvariant();
        var isExplicitNoReply = IsExplicitNoReplySituation(situationType);
        var preferredFamilies = GetPreferredMoveFamilies(situationType);

        var alternatives = scores
            .Where(s => !ReferenceEquals(s, winner))
            .Where(s => !string.Equals(s.Candidate.Move, winner.Candidate.Move, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(s.Candidate.Reply, winner.Candidate.Reply, StringComparison.Ordinal))
            .Where(IsQualified)
            .Where(s => isExplicitNoReply || !IsNoReply(s))
            .OrderByDescending(s => IsPreferredFamily(s, preferredFamilies))
            .ThenByDescending(s => s.ComputedTotal)
            .Take(2)
            .Select(s => s.Candidate)
            .ToArray();

        return alternatives;
    }

    private static string[] GetPreferredMoveFamilies(string situationType)
    {
        situationType = (situationType ?? string.Empty).Trim().ToLowerInvariant();

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
            "cta_or_question" => new[] { "answer_supportively", "add_specific_insight", "ask_relevant_question" },
            "question" => new[] { "answer_supportively", "ask_relevant_question" },
            "greeting" => new[] { "respond", "appreciate" },
            "achievement" => new[] { "praise", "congratulate", "appreciate" },
            "news" => new[] { "add_insight", "ask_relevant_question", "appreciate" },
            "update" => new[] { "acknowledge", "appreciate" },
            _ => new[] { "engage", "appreciate", "light_touch", "add_insight" }
        };
    }

    private static bool IsExplicitNoReplySituation(string situationType)
    {
        situationType = (situationType ?? string.Empty).Trim().ToLowerInvariant();

        return situationType is
            "defer_no_reply" or
            "controversial_no_reply" or
            "low_signal" or
            "sensitive" or
            "controversial";
    }

    private static bool IsNoReply(CandidateScore score)
    {
        return string.Equals(
            score.Candidate.Move,
            "no_reply",
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPreferredFamily(CandidateScore score, string[] preferredFamilies)
    {
        return preferredFamilies.Contains(
            score.Candidate.Move,
            StringComparer.OrdinalIgnoreCase);
    }

    private static bool HasUsableReply(CandidateScore score)
    {
        if (IsNoReply(score))
            return true;

        return !string.IsNullOrWhiteSpace(score.Candidate.Reply);
    }

    private static bool IsQualified(CandidateScore score)
    {
        if (!string.IsNullOrWhiteSpace(score.DisqualifiedReason))
            return false;

        if (score.ComputedTotal <= 0.0)
            return false;

        if (score.HallucinationPenalty >= 0.45)
            return false;

        if (score.GenericPenalty >= 0.60)
            return false;

        if (score.GenericPraisePenalty >= 0.65)
            return false;

        return HasUsableReply(score);
    }

    private static CandidateScore PickBest(IEnumerable<CandidateScore> scores)
    {
        return scores
            .OrderByDescending(s => s.ComputedTotal)
            .ThenByDescending(s => s.FamilyMatchBoost)
            .ThenByDescending(s => s.Specificity)
            .ThenByDescending(s => s.Relevance)
            .ThenByDescending(s => s.InsightDepth)
            .ThenByDescending(s => s.CTAResponseQuality)
            .ThenByDescending(s => s.PositioningStrength)
            .ThenBy(s => s.GenericPenalty)
            .ThenBy(s => s.GenericPraisePenalty)
            .ThenBy(s => s.EngagementCost)
            .First();
    }

    private static CandidateScore BuildFallback(
        IReadOnlyList<CandidateScore> scores,
        bool isExplicitNoReply)
    {
        if (isExplicitNoReply)
        {
            var noReply = scores.FirstOrDefault(IsNoReply);

            if (noReply != null)
                return noReply;

            return new CandidateScore
            {
                Candidate = new SocialMoveCandidate
                {
                    Move = "no_reply",
                    Reply = string.Empty,
                    Rationale = "Explicit no-reply situation."
                },
                ComputedTotal = 0.0,
                DisqualifiedReason = "explicit no-reply fallback"
            };
        }

        var bestNonNoReply = scores
            .Where(s => !IsNoReply(s))
            .Where(s => !string.IsNullOrWhiteSpace(s.Candidate.Reply))
            .OrderByDescending(s => s.ComputedTotal)
            .FirstOrDefault();

        if (bestNonNoReply != null)
            return bestNonNoReply;

        return new CandidateScore
        {
            Candidate = new SocialMoveCandidate
            {
                Move = "light_touch",
                Reply = "There’s a clear signal here worth engaging with.",
                Rationale = "Safe non-empty fallback because no qualified candidate survived."
            },
            ComputedTotal = 0.01,
            DisqualifiedReason = "non-empty fallback"
        };
    }
}
