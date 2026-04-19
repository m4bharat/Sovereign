using Sovereign.Domain.Models;
using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Services;

public sealed class WinnerSelectionEngine : IWinnerSelectionEngine
{
    public WinnerSelectionResult SelectBest(IReadOnlyList<CandidateScore> scoredCandidates, MessageContext context)
    {
        var situationType = (context.SituationType ?? "").ToLowerInvariant();

        // ===== FAMILY-PRE-FILTER (task: family first) =====
        var preferredFamilies = GetPreferredFamilies(situationType);
        var familyCandidates = scoredCandidates.Where(s => preferredFamilies.Contains(s.Candidate.Move.ToLowerInvariant()));

        if (familyCandidates.Any())
        {
            // Within family, best score
            var familyBest = familyCandidates.OrderByDescending(s => s.ComputedTotal).First().Candidate;
            return new WinnerSelectionResult
            {
                Winner = familyBest,
                Alternatives = scoredCandidates
                    .Where(s => s.Candidate.Move != familyBest.Move)
                    .OrderByDescending(s => s.ComputedTotal)
                    .Take(2)
                    .Select(s => s.Candidate)
                    .ToArray()
            };
        }

        // Fallback: highest total passing gates
        var viable = scoredCandidates.Where(s => s.ComputedTotal > 0.0).OrderByDescending(s => s.ComputedTotal).ToArray();

        if (viable.Length == 0)
        {
            return new WinnerSelectionResult
            {
                Winner = new SocialMoveCandidate { Move = "no_reply", Rationale = "No viable candidates" },
                Alternatives = Array.Empty<SocialMoveCandidate>()
            };
        }

        var winner = viable[0].Candidate;
        var alternatives = viable.Skip(1).Take(2).Select(s => s.Candidate).ToArray();

        return new WinnerSelectionResult { Winner = winner, Alternatives = alternatives };
    }

    private static string[] GetPreferredFamilies(string situationType)
    {
        return situationType switch
        {
            "achievement_share" => new[] { "praise" },
            "personal_update" => new[] { "congratulate" },
            "cta_engagement" => new[] { "answer_supportively" },
            "defer_no_reply" or "controversial_no_reply" => new[] { "no_reply" },
            _ => new[] { "appreciate" }
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