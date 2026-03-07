using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Services;

public sealed class AIStrategyService : IAIStrategyService
{
    private readonly IRelationshipIntelligenceEngine _relationshipIntelligenceEngine;
    private readonly IInteractionEngine _interactionEngine;

    public AIStrategyService(
        IRelationshipIntelligenceEngine relationshipIntelligenceEngine,
        IInteractionEngine interactionEngine)
    {
        _relationshipIntelligenceEngine = relationshipIntelligenceEngine;
        _interactionEngine = interactionEngine;
    }

    public StrategyResult GenerateStrategy(RelationshipContext context)
    {
        var insight = _relationshipIntelligenceEngine.Analyze(context);
        var suggestion = _interactionEngine.GenerateSuggestion(context, insight);

        return new StrategyResult
        {
            Insight = insight,
            Suggestion = suggestion
        };
    }
}
