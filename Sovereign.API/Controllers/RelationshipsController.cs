using Microsoft.AspNetCore.Mvc;
using Sovereign.Application.DTOs;
using Sovereign.Application.UseCases;

namespace Sovereign.API.Controllers;

[ApiController]
[Route("api/relationships")]
public sealed class RelationshipsController : ControllerBase
{
    private readonly CreateRelationshipUseCase _createRelationshipUseCase;
    private readonly LogInteractionUseCase _logInteractionUseCase;
    private readonly GenerateStrategyUseCase _generateStrategyUseCase;

    public RelationshipsController(
        CreateRelationshipUseCase createRelationshipUseCase,
        LogInteractionUseCase logInteractionUseCase,
        GenerateStrategyUseCase generateStrategyUseCase)
    {
        _createRelationshipUseCase = createRelationshipUseCase;
        _logInteractionUseCase = logInteractionUseCase;
        _generateStrategyUseCase = generateStrategyUseCase;
    }

    [HttpPost]
    public async Task<ActionResult<CreateRelationshipResponse>> Create(
        [FromBody] CreateRelationshipRequest request,
        CancellationToken ct)
    {
        var response = await _createRelationshipUseCase.ExecuteAsync(request, ct);
        return Ok(response);
    }

    [HttpPost("{relationshipId:guid}/interactions")]
    public async Task<ActionResult<LogInteractionResponse>> LogInteraction(
        [FromRoute] Guid relationshipId,
        CancellationToken ct)
    {
        var response = await _logInteractionUseCase.ExecuteAsync(relationshipId, ct);
        return Ok(response);
    }

    [HttpGet("{relationshipId:guid}/strategy")]
    public async Task<ActionResult<GenerateStrategyResponse>> GenerateStrategy(
        [FromRoute] Guid relationshipId,
        CancellationToken ct)
    {
        var response = await _generateStrategyUseCase.ExecuteAsync(relationshipId, ct);
        return Ok(response);
    }
}
