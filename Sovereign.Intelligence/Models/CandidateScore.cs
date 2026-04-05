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
    public double QuestionQuality { get; init; }

    private double? _totalOverride;

    public double Total
    {
        get => _totalOverride ??
               (0.22 * Relevance) +
               (0.16 * SocialFit) +
               (0.16 * Specificity) +
               (0.16 * InsightDepth) +
               (0.10 * RelationshipFit) +
               (0.06 * QuestionQuality) +
               (0.06 * Tone) +
               (0.04 * Brevity) +
               (0.06 * TimingFit) -
               (0.18 * HallucinationPenalty) -
               (0.12 * GenericPraisePenalty) -
               (0.08 * EngagementCost);
        set => _totalOverride = value;
    }
}
