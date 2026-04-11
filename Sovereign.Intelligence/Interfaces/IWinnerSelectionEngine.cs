using Sovereign.Domain.Models;
using Sovereign.Intelligence.Models;
using Sovereign.Intelligence.Services;

namespace Sovereign.Intelligence.Interfaces;

public interface IWinnerSelectionEngine
{
    WinnerSelectionResult SelectBest(IReadOnlyList<CandidateScore> scoredCandidates, MessageContext context);
}
