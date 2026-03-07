namespace Sovereign.Application.DTOs;

public sealed class GenerateStrategyResponse
{
    public Guid RelationshipId { get; init; }
    public double RelationshipStrengthScore { get; init; }
    public double OpportunityScore { get; init; }
    public double RiskScore { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string RecommendedAction { get; init; } = string.Empty;
    public string RecommendedStance { get; init; } = string.Empty;
    public string DraftPrompt { get; init; } = string.Empty;
    public string Reasoning { get; init; } = string.Empty;
}
