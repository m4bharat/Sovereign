namespace Sovereign.Intelligence.Models;

public sealed class SocialMoveCandidate
{
    public string Move { get; init; } = string.Empty;
    public string Reply { get; init; } = string.Empty;
    public double GenerationConfidence { get; init; }
    public string Rationale { get; init; } = string.Empty;
}
