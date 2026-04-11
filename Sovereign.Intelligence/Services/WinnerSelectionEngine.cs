using Sovereign.Domain.Models;
using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Services;

public sealed class WinnerSelectionEngine : IWinnerSelectionEngine
{
    public WinnerSelectionResult SelectBest(IReadOnlyList<CandidateScore> scoredCandidates, MessageContext context)
    {
        var filtered = scoredCandidates
            .Where(score => score.HallucinationPenalty < 0.35)
            .Where(score => score.Tone >= 0.20)
            .Where(score => score.Total >= 0.45)
            .Where(score => !IsLowQualityQuestion(score))
            .Where(score => !IsDisqualifiedForCtaPost(score.Candidate, context, score))
            .Where(score => !IsCtaEngagementPost(context) || MeetsCtaThresholds(score))
            .OrderByDescending(score => score.Total)
            .ThenByDescending(score => score.PositioningStrength)
            .ThenByDescending(score => score.CTAResponseQuality)
            .ThenByDescending(score => score.InsightDepth)
            .ThenBy(score => score.ParticipationWithoutPositionPenalty)
            .ThenByDescending(score => score.RiskAdjustedValue)
            .ThenByDescending(score => score.Specificity)
            .ThenBy(score => score.GenericPraisePenalty)
            .ThenBy(score => score.HallucinationPenalty)
            .ToArray();

        if (filtered.Length == 0)
        {
            return new WinnerSelectionResult
            {
                Winner = new SocialMoveCandidate
                {
                    Move = "no_reply",
                    Rationale = "No candidate met the minimum threshold for replying."
                },
                Alternatives = Array.Empty<SocialMoveCandidate>()
            };
        }

        var winner = filtered[0].Candidate;

        var hasDraft = !string.IsNullOrWhiteSpace(context.Message);
        var isFeedReply = string.Equals(context.Surface, "feed_reply", StringComparison.OrdinalIgnoreCase);
        var isCompose = string.Equals(context.Surface, "start_post", StringComparison.OrdinalIgnoreCase);

        if (string.Equals(winner.Move, "no_reply", StringComparison.OrdinalIgnoreCase) &&
            hasDraft &&
            (isFeedReply || isCompose))
        {
            var fallback = filtered
                .FirstOrDefault(c => !string.Equals(c.Candidate.Move, "no_reply", StringComparison.OrdinalIgnoreCase));

            if (fallback != null)
            {
                winner = fallback.Candidate;
            }
        }

        var alternatives = filtered
            .Where(score => !ReferenceEquals(score.Candidate, winner))
            .Select(score => score.Candidate)
            .Take(2)
            .ToArray();

        return new WinnerSelectionResult
        {
            Winner = winner,
            Alternatives = alternatives
        };
    }

    private static bool IsLowQualityQuestion(CandidateScore score)
    {
        if (score.Candidate.Move != "ask_relevant_question")
            return false;

        var reply = (score.Candidate.Reply ?? string.Empty).Trim().ToLowerInvariant();

        if (IsBareQuestion(reply) && RequiresFraming(score))
            return true;

        var genericStems = new[]
        {
            "what do you think",
            "can you share more",
            "would love to hear",
            "what has your experience been",
            "what's the biggest challenge"
        };

        return genericStems.Any(stem => reply.Contains(stem));
    }

    private static bool IsBareQuestion(string reply)
    {
        var text = reply.Trim();
        if (!text.EndsWith("?"))
            return false;

        return !text.Contains(".") && !text.Contains("\n\n");
    }

    private static bool RequiresFraming(CandidateScore score)
    {
        return false;
    }

    private static readonly string[] CtaSignals =
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

    private static bool IsCtaEngagementPost(MessageContext context)
    {
        var source = string.Join(" ",
            context.SourceTitle ?? string.Empty,
            context.SourceText ?? string.Empty,
            context.ParentContextText ?? string.Empty,
            context.NearbyContextText ?? string.Empty)
            .ToLowerInvariant();

        return CtaSignals.Any(signal => source.Contains(signal));
    }

    private static bool MeetsCtaThresholds(CandidateScore score)
    {
        return score.CTAResponseQuality >= 0.24 &&
               (score.PositioningStrength >= 0.18 || score.InsightDepth >= 0.18) &&
               score.ParticipationWithoutPositionPenalty <= 0.40;
    }

    private static bool IsDisqualifiedForCtaPost(
        SocialMoveCandidate candidate,
        MessageContext context,
        CandidateScore score)
    {
        if (!IsCtaEngagementPost(context))
            return false;

        var reply = (candidate.Reply ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(reply))
            return false;

        if (score.ParticipationWithoutPositionPenalty >= 0.45 &&
            score.PositioningStrength <= 0.16 &&
            score.InsightDepth <= 0.16)
        {
            return true;
        }

        if (reply.StartsWith("Great question", StringComparison.OrdinalIgnoreCase) &&
            score.PositioningStrength <= 0.18 &&
            score.CTAResponseQuality <= 0.30)
        {
            return true;
        }

        return false;
    }
}