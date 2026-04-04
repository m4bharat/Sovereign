namespace Sovereign.Intelligence.Models;

public sealed class CandidateScore
{
    public SocialMoveCandidate Candidate { get; init; } = new();
    public double Relevance { get; init; }
    public double SocialFit { get; init; }
    public double Specificity { get; init; }
    public double HallucinationPenalty { get; init; }
    public double Tone { get; init; }
    public double Brevity { get; init; }
    public double RelationshipFit { get; init; }
    public double RiskAdjustedValue { get; init; }
    public double TimingFit { get; init; }
    public double InsightDepth { get; init; }
    public double GenericPraisePenalty { get; init; }
    public double EngagementCost { get; init; }

    public double Total { get; set; }
}
