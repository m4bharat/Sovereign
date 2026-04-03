namespace Sovereign.Intelligence.DecisionV2;

public sealed class DecisionV2Result
{
    public string Strategy { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Reply { get; set; } = string.Empty;
    public string Move { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    public bool ShouldReplyNow { get; set; } = true;
    public bool ShouldReply { get; set; } = true;
    public List<string> Alternatives { get; set; } = new();
    public string RelationshipEffect { get; set; } = string.Empty;
    public List<string> MemorySignals { get; set; } = new();
    public string FollowUpSuggestion { get; set; } = string.Empty;
    public string SituationType { get; set; } = string.Empty;
    public double RiskScore { get; set; } = 0.0;
    public double OpportunityScore { get; set; } = 0.0;
}
