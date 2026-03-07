using Sovereign.Application.DTOs;
using Sovereign.Application.Interfaces;
using Sovereign.Application.Mappers;
using Sovereign.Intelligence.Interfaces;

namespace Sovereign.Application.UseCases;

public sealed class GenerateStrategyUseCase
{
    private readonly IRelationshipRepository _repository;
    private readonly IAIStrategyService _strategyService;

    public GenerateStrategyUseCase(
        IRelationshipRepository repository,
        IAIStrategyService strategyService)
    {
        _repository = repository;
        _strategyService = strategyService;
    }

    public async Task<GenerateStrategyResponse> ExecuteAsync(Guid relationshipId, CancellationToken ct = default)
    {
        var relationship = await _repository.GetByIdAsync(relationshipId, ct)
            ?? throw new InvalidOperationException("Relationship not found.");

        var context = RelationshipContextMapper.Map(relationship);
        var result = _strategyService.GenerateStrategy(context);

        return new GenerateStrategyResponse
        {
            RelationshipId = relationship.Id,
            RelationshipStrengthScore = result.Insight.RelationshipStrengthScore,
            OpportunityScore = result.Insight.OpportunityScore,
            RiskScore = result.Insight.RiskScore,
            Summary = result.Insight.Summary,
            RecommendedAction = result.Suggestion.RecommendedAction,
            RecommendedStance = result.Suggestion.RecommendedStance,
            DraftPrompt = result.Suggestion.DraftPrompt,
            Reasoning = result.Suggestion.Reasoning
        };
    }
}
