namespace Sovereign.Intelligence.Models;

public sealed class SocialMoveCandidate
{
    public string Move { get; init; } = string.Empty;
    public string Reply { get; set; } = string.Empty;
    public double GenerationConfidence { get; set; }
    public string Rationale { get; init; } = string.Empty;
    public string ShortReply { get; init; } = string.Empty;
    public bool RequiresPolish { get; init; }
    public IReadOnlyList<string> Alternatives { get; set; } = Array.Empty<string>();
    public string RelationshipEffect { get; init; } = string.Empty;
    public double RiskScore { get; init; } = 0.0;
    public double OpportunityScore { get; init; } = 0.0;
}
