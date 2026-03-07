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

        var summary = BuildSummary(context, strengthScore, opportunityScore, riskScore);

        return new SocialInsight
        {
            RelationshipStrengthScore = Math.Round(Clamp(strengthScore, 0, 100), 2),
            OpportunityScore = Math.Round(Clamp(opportunityScore, 0, 100), 2),
            RiskScore = Math.Round(Clamp(riskScore, 0, 100), 2),
            Summary = summary
        };
    }

    private static string BuildSummary(RelationshipContext context, double strength, double opportunity, double risk)
    {
        if (risk >= 55)
            return $"Relationship with {context.Role} is at risk of decay. A calibrated follow-up is recommended now.";

        if (opportunity >= 50)
            return $"Relationship with {context.Role} has active upside. Strategic engagement could increase leverage.";

        if (strength >= 60)
            return $"Relationship with {context.Role} is healthy. Maintain momentum with light-touch consistency.";

        return $"Relationship with {context.Role} is stable but underdeveloped. A deliberate touchpoint could strengthen it.";
    }

    private static double Clamp(double value, double min, double max)
        => Math.Min(max, Math.Max(min, value));
}
