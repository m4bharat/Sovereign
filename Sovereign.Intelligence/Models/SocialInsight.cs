namespace Sovereign.Intelligence.Models;

public sealed class SocialInsight
{
    public double RelationshipStrengthScore { get; init; }
    public double OpportunityScore { get; init; }
    public double RiskScore { get; init; }
    public string Summary { get; init; } = string.Empty;
}
