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
                    context.Role.ToString(),
                    context.LastTopicSummary,
                    "Warm-Strategic"),
                Reasoning = "High decay risk detected."
            };
        }

        if (insight.OpportunityScore >= 50)
        {
            return new InteractionSuggestion
            {
                RecommendedAction = "Advance",
                RecommendedStance = "Strategic",
                DraftPrompt = _promptTemplateProvider.BuildMaintainPrompt(
                    context.Role.ToString(),
                    context.LastTopicSummary,
                    "Strategic"),
                Reasoning = "Opportunity signal is high."
            };
        }

        return new InteractionSuggestion
        {
            RecommendedAction = "Maintain",
            RecommendedStance = "Light-Warm",
            DraftPrompt = _promptTemplateProvider.BuildMaintainPrompt(
                context.Role.ToString(),
                context.LastTopicSummary,
                "Light-Warm"),
            Reasoning = "Relationship is stable."
        };
    }
}
