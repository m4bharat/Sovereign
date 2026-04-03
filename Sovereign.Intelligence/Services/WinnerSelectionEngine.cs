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
            .Where(score => score.Total >= 0.60) // Minimum threshold for replying
            .OrderByDescending(score => score.Total)
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
}

public sealed class WinnerSelectionResult
{
    public SocialMoveCandidate Winner { get; set; } = new();
    public IReadOnlyList<SocialMoveCandidate> Alternatives { get; set; } = Array.Empty<SocialMoveCandidate>();
}
