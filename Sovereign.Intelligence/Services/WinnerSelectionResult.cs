namespace Sovereign.Intelligence.Services;

using Sovereign.Intelligence.Models;

public sealed class WinnerSelectionResult
{
    public SocialMoveCandidate Winner { get; set; } = null!;
    public IReadOnlyList<SocialMoveCandidate> Alternatives { get; set; } = Array.Empty<SocialMoveCandidate>();
}