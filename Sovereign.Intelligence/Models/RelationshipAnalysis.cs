namespace Sovereign.Intelligence.Models;

public sealed class RelationshipAnalysis
{
    public double ReciprocityScore { get; init; } = 0.0;
    public double MomentumScore { get; init; } = 0.0;
    public double PowerDifferential { get; init; } = 0.0;
    public double EmotionalTemperature { get; init; } = 0.0;
    public double RiskScore { get; init; } = 0.0;
    public double OpportunityScore { get; init; } = 0.0;
    public double ReplyUrgencyHint { get; init; } = 0.0;
}
