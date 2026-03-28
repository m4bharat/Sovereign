using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Services;

public sealed class WinnerSelectionEngine
{
    public SocialMoveCandidate SelectBest(IReadOnlyList<CandidateScore> scoredCandidates)
    {
        var filtered = scoredCandidates
            .Where(score => score.HallucinationPenalty < 0.35)
            .Where(score => score.Tone >= 0.20)
            .OrderByDescending(score => score.Total)
            .ToArray();

        if (filtered.Length > 0)
        {
            return filtered[0].Candidate;
        }

        return scoredCandidates
            .OrderByDescending(score => score.Total)
            .Select(score => score.Candidate)
            .FirstOrDefault() ?? new SocialMoveCandidate
            {
                Move = "appreciate",
                Reply = "Appreciate you sharing this."
            };
    }
}
