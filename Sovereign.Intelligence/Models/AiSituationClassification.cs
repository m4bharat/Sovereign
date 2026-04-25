namespace Sovereign.Intelligence.Models;

public sealed class AiSituationClassification
{
    public string SituationType { get; init; } = "general";
    public string UserIntent { get; init; } = "generate_reply";
    public string RecommendedMove { get; init; } = "engage";
    public bool IsCommandOnly { get; init; }
    public double Confidence { get; init; }
    public string Rationale { get; init; } = string.Empty;
}
