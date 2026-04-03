using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Engines;

public sealed class RelationshipIntelligenceEngine : IRelationshipIntelligenceEngine
{
    public SocialInsight Analyze(RelationshipContext context)
    {
        var strengthScore =
            (context.TotalInteractions * 1.5)
            + (context.ReciprocityScore * 25)
            + (context.MomentumScore * 15)
            - (context.SilenceDays * 0.8);

        var opportunityScore =
            (1 + context.PowerDifferential) * 10
            + (context.EmotionalTemperature * 5)
            + Math.Max(0, 20 - context.SilenceDays);

        var riskScore =
            (context.SilenceDays * 1.2)
            + ((1 - context.ReciprocityScore) * 20)
            + (Math.Max(0, context.PowerDifferential) * 8);

        return new SocialInsight
        {
            OpportunityScore = Math.Round(Math.Clamp(opportunityScore, 0, 100), 2),
            RiskScore = Math.Round(Math.Clamp(riskScore, 0, 100), 2),
            Summary = riskScore >= 55
                ? $"Relationship with {context.Role} is at risk of decay."
                : opportunityScore >= 50
                    ? $"Relationship with {context.Role} has active upside."
                    : $"Relationship with {context.Role} is stable."
        };
    }
}
