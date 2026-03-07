using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Engines;

public sealed class InteractionEngine : IInteractionEngine
{
    private readonly IPromptTemplateProvider _promptTemplateProvider;

    public InteractionEngine(IPromptTemplateProvider promptTemplateProvider)
    {
        _promptTemplateProvider = promptTemplateProvider;
    }

    public InteractionSuggestion GenerateSuggestion(RelationshipContext context, SocialInsight insight)
    {
        if (insight.RiskScore >= 55)
        {
            return new InteractionSuggestion
            {
                RecommendedAction = "Reconnect",
                RecommendedStance = "Warm-Strategic",
                DraftPrompt = _promptTemplateProvider.BuildReconnectPrompt(
                    context.Role,
                    context.LastTopicSummary,
                    "Warm-Strategic"),
                Reasoning = "High decay risk detected due to silence and weak reciprocity."
            };
        }

        if (insight.OpportunityScore >= 50)
        {
            return new InteractionSuggestion
            {
                RecommendedAction = "Advance",
                RecommendedStance = "Strategic",
                DraftPrompt = _promptTemplateProvider.BuildMaintainPrompt(
                    context.Role,
                    context.LastTopicSummary,
                    "Strategic"),
                Reasoning = "Opportunity signal is high. A value-forward message is likely beneficial."
            };
        }

        return new InteractionSuggestion
        {
            RecommendedAction = "Maintain",
            RecommendedStance = "Light-Warm",
            DraftPrompt = _promptTemplateProvider.BuildMaintainPrompt(
                context.Role,
                context.LastTopicSummary,
                "Light-Warm"),
            Reasoning = "Relationship is stable. Maintain presence without over-investing."
        };
    }
}
