namespace Sovereign.Intelligence.Interfaces;

using Sovereign.Intelligence.Models;
using Sovereign.Intelligence.Services;

public interface IWinnerSelectionEngine
{
    WinnerSelectionResult SelectBest(IReadOnlyList<CandidateScore> scoredCandidates);
}
