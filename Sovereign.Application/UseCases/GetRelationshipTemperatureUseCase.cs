using Sovereign.Application.DTOs;
using Sovereign.Application.Engines;
using Sovereign.Application.Interfaces;

namespace Sovereign.Application.UseCases;

public sealed class GetRelationshipTemperatureUseCase
{
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly RelationshipTemperatureEngine _relationshipTemperatureEngine;

    public GetRelationshipTemperatureUseCase(
        IRelationshipRepository relationshipRepository,
        RelationshipTemperatureEngine relationshipTemperatureEngine)
    {
        _relationshipRepository = relationshipRepository;
        _relationshipTemperatureEngine = relationshipTemperatureEngine;
    }

    public async Task<RelationshipTemperatureResponse> ExecuteAsync(Guid relationshipId, CancellationToken ct = default)
    {
        var relationship = await _relationshipRepository.GetByIdAsync(relationshipId, ct)
            ?? throw new InvalidOperationException("Relationship not found.");

        var result = _relationshipTemperatureEngine.Calculate(relationship);

        return new RelationshipTemperatureResponse
        {
            RelationshipId = relationship.Id,
            Score = result.Score,
            SilenceDays = result.SilenceDays,
            Temperature = result.Temperature,
            RecommendedAction = result.RecommendedAction
        };
    }
}
