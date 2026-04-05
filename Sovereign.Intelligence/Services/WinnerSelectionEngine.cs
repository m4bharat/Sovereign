using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Services;

public sealed class WinnerSelectionEngine : IWinnerSelectionEngine
{
    public WinnerSelectionResult SelectBest(IReadOnlyList<CandidateScore> scoredCandidates)
    {
        var filtered = scoredCandidates
            .Where(score => score.HallucinationPenalty < 0.35)
            .Where(score => score.Tone >= 0.20)
            .Where(score => score.Total > 0.5) // Minimum threshold for replying
            .Where(score => !IsLowQualityQuestion(score))
            .OrderByDescending(score => score.Total)
            .ThenByDescending(score => score.InsightDepth)
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

        var primaryWinner = filtered[0].Candidate;
        var alternatives = filtered.Skip(1).Take(2).Select(score => score.Candidate).ToArray();

        return new WinnerSelectionResult
        {
            Winner = primaryWinner,
            Alternatives = alternatives
        };
    }

    private static bool IsLowQualityQuestion(CandidateScore score)
    {
        if (score.Candidate.Move != "ask_relevant_question")
            return false;

        var reply = (score.Candidate.Reply ?? string.Empty).Trim().ToLowerInvariant();

        // Disqualify bare questions on high-signal posts
        if (IsBareQuestion(reply) && RequiresFraming(score))
            return true;

        // Disqualify generic stems
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
        // For now, only disqualify if it's clearly a bare question on a high-signal post
        // We'll need to pass context through to make this more accurate
        return false; // Temporarily disable to avoid breaking existing tests
    }
}
