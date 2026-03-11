using Sovereign.Application.DTOs;
using Sovereign.Application.Interfaces;
using Sovereign.Application.Engines;
using Sovereign.Application.Services;

namespace Sovereign.Application.UseCases;

public sealed class GetDashboardOverviewUseCase
{
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly IMemoryRepository _memoryRepository;
    private readonly DecayScoringEngine _decayScoringEngine;
    private readonly RelationshipTemperatureEngine _temperatureEngine;
    private readonly FollowUpSuggestionService _followUpSuggestionService;

    public GetDashboardOverviewUseCase(IRelationshipRepository relationshipRepository, IMemoryRepository memoryRepository, DecayScoringEngine decayScoringEngine, RelationshipTemperatureEngine temperatureEngine, FollowUpSuggestionService followUpSuggestionService)
    {
        _relationshipRepository = relationshipRepository;
        _memoryRepository = memoryRepository;
        _decayScoringEngine = decayScoringEngine;
        _temperatureEngine = temperatureEngine;
        _followUpSuggestionService = followUpSuggestionService;
    }

    public async Task<DashboardOverviewResponse> ExecuteAsync(string userId, CancellationToken ct = default)
    {
        var relationships = await _relationshipRepository.GetByUserIdAsync(userId, ct);
        var memories = await _memoryRepository.GetByUserIdAsync(userId, ct);

        var temperatureItems = relationships.Select(relationship => new
        {
            Relationship = relationship,
            Temperature = _temperatureEngine.Calculate(relationship),
            DecayScore = Math.Round(_decayScoringEngine.Calculate(relationship), 2),
            DaysSilent = (DateTime.UtcNow - relationship.LastInteractionAtUtc).Days
        }).ToList();

        var priorityAlerts = temperatureItems
            .Where(x => x.DaysSilent >= 7 || x.DecayScore >= 10d || x.Temperature.Temperature == "Cold")
            .OrderByDescending(x => x.DecayScore)
            .Take(5)
            .Select(x => new DecayAlertDto
            {
                RelationshipId = x.Relationship.Id,
                ContactId = x.Relationship.ContactId,
                DaysSilent = x.DaysSilent,
                DecayScore = x.DecayScore,
                Temperature = x.Temperature.Temperature,
                SuggestedAction = x.Temperature.RecommendedAction,
                SuggestedMessage = _followUpSuggestionService.BuildSuggestedMessage(x.Relationship, x.Temperature.RecommendedAction)
            }).ToList();

        return new DashboardOverviewResponse
        {
            RelationshipCount = relationships.Count,
            HotRelationships = temperatureItems.Count(x => x.Temperature.Temperature == "Hot"),
            WarmRelationships = temperatureItems.Count(x => x.Temperature.Temperature == "Warm"),
            ColdRelationships = temperatureItems.Count(x => x.Temperature.Temperature == "Cold"),
            OpenDecayAlerts = priorityAlerts.Count,
            MemoryCount = memories.Count,
            PriorityAlerts = priorityAlerts,
            RecentMemories = memories.OrderByDescending(x => x.CreatedAtUtc).Take(5).Select(x => new MemoryEntryDto
            {
                Id = x.Id,
                Key = x.Key,
                Value = x.Value,
                CreatedAtUtc = x.CreatedAtUtc
            }).ToList()
        };
    }
}
