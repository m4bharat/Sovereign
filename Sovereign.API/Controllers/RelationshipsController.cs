using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sovereign.Application.DTOs;
using Sovereign.Application.UseCases;

namespace Sovereign.API.Controllers;

[ApiController]
[Authorize]
[Route("api/relationships")]
public sealed class RelationshipsController : ControllerBase
{
    private readonly CreateRelationshipUseCase _createRelationshipUseCase;
    private readonly LogInteractionUseCase _logInteractionUseCase;
    private readonly RecordOutcomeUseCase _recordOutcomeUseCase;
    private readonly GenerateStrategyUseCase _generateStrategyUseCase;
    private readonly GetRelationshipTemperatureUseCase _getRelationshipTemperatureUseCase;
    private readonly GetDecayAlertsUseCase _getDecayAlertsUseCase;

    public RelationshipsController(CreateRelationshipUseCase createRelationshipUseCase, LogInteractionUseCase logInteractionUseCase, RecordOutcomeUseCase recordOutcomeUseCase, GenerateStrategyUseCase generateStrategyUseCase, GetRelationshipTemperatureUseCase getRelationshipTemperatureUseCase, GetDecayAlertsUseCase getDecayAlertsUseCase)
    {
        _createRelationshipUseCase = createRelationshipUseCase;
        _logInteractionUseCase = logInteractionUseCase;
        _recordOutcomeUseCase = recordOutcomeUseCase;
        _generateStrategyUseCase = generateStrategyUseCase;
        _getRelationshipTemperatureUseCase = getRelationshipTemperatureUseCase;
        _getDecayAlertsUseCase = getDecayAlertsUseCase;
    }

    [HttpPost]
    public async Task<ActionResult<CreateRelationshipResponse>> Create([FromBody] CreateRelationshipRequest request, CancellationToken ct)
    {
        var response = await _createRelationshipUseCase.ExecuteAsync(request, ct);
        return Ok(response);
    }

    [HttpPost("{relationshipId:guid}/interactions")]
    public async Task<ActionResult<LogInteractionResponse>> LogInteraction([FromRoute] Guid relationshipId, CancellationToken ct)
    {
        var response = await _logInteractionUseCase.ExecuteAsync(relationshipId, ct);
        return Ok(response);
    }

    [HttpPost("{relationshipId:guid}/outcomes")]
    public async Task<ActionResult<RecordOutcomeResponse>> RecordOutcome([FromRoute] Guid relationshipId, [FromBody] RecordOutcomeRequest request, CancellationToken ct)
    {
        var response = await _recordOutcomeUseCase.ExecuteAsync(relationshipId, request, ct);
        return Ok(response);
    }

    [HttpGet("{relationshipId:guid}/strategy")]
    public async Task<ActionResult<GenerateStrategyResponse>> GenerateStrategy([FromRoute] Guid relationshipId, CancellationToken ct)
    {
        var response = await _generateStrategyUseCase.ExecuteAsync(relationshipId, ct);
        return Ok(response);
    }

    [HttpGet("{relationshipId:guid}/temperature")]
    public async Task<ActionResult<RelationshipTemperatureResponse>> GetTemperature([FromRoute] Guid relationshipId, CancellationToken ct)
    {
        var response = await _getRelationshipTemperatureUseCase.ExecuteAsync(relationshipId, ct);
        return Ok(response);
    }

    [HttpGet("decay-alerts")]
    public async Task<ActionResult<DecayAlertsResponse>> GetDecayAlerts([FromQuery] string userId, CancellationToken ct)
    {
        var response = await _getDecayAlertsUseCase.ExecuteAsync(userId, ct);
        return Ok(response);
    }
}
