using Sovereign.Application.DTOs;
using Sovereign.Application.Engines;
using Sovereign.Application.Interfaces;
using Sovereign.Application.Services;

namespace Sovereign.Application.UseCases;

public sealed class GetDecayAlertsUseCase
{
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly DecayScoringEngine _decayScoringEngine;
    private readonly RelationshipTemperatureEngine _relationshipTemperatureEngine;
    private readonly FollowUpSuggestionService _followUpSuggestionService;

    public GetDecayAlertsUseCase(
        IRelationshipRepository relationshipRepository,
        DecayScoringEngine decayScoringEngine,
        RelationshipTemperatureEngine relationshipTemperatureEngine,
        FollowUpSuggestionService followUpSuggestionService)
    {
        _relationshipRepository = relationshipRepository;
        _decayScoringEngine = decayScoringEngine;
        _relationshipTemperatureEngine = relationshipTemperatureEngine;
        _followUpSuggestionService = followUpSuggestionService;
    }

    public async Task<DecayAlertsResponse> ExecuteAsync(string userId, CancellationToken ct = default)
    {
        var relationships = await _relationshipRepository.GetByUserIdAsync(userId, ct);

        var alerts = relationships
            .Select(relationship =>
            {
                var daysSilent = (DateTime.UtcNow - relationship.LastInteractionAtUtc).Days;
                var decayScore = Math.Round(_decayScoringEngine.Calculate(relationship), 2);
                var temperature = _relationshipTemperatureEngine.Calculate(relationship);

                return new
                {
                    Relationship = relationship,
                    DaysSilent = daysSilent,
                    DecayScore = decayScore,
                    Temperature = temperature
                };
            })
            .Where(x => x.DaysSilent >= 7 || x.DecayScore >= 10d || x.Temperature.Temperature == "Cold")
            .OrderByDescending(x => x.DecayScore)
            .Select(x => new DecayAlertDto
            {
                RelationshipId = x.Relationship.Id,
                ContactId = x.Relationship.ContactId,
                DaysSilent = x.DaysSilent,
                DecayScore = x.DecayScore,
                Temperature = x.Temperature.Temperature,
                SuggestedAction = x.Temperature.RecommendedAction,
                SuggestedMessage = _followUpSuggestionService.BuildSuggestedMessage(x.Relationship, x.Temperature.RecommendedAction)
            })
            .ToList();

        return new DecayAlertsResponse
        {
            Alerts = alerts
        };
    }
}
