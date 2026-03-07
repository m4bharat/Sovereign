using Microsoft.AspNetCore.Mvc;
using Sovereign.Application.DTOs;
using Sovereign.Application.UseCases;

namespace Sovereign.API.Controllers;

[ApiController]
[Route("api/social-graph")]
public sealed class SocialGraphController : ControllerBase
{
    private readonly UpsertSocialEdgeUseCase _upsertSocialEdgeUseCase;
    private readonly CaptureInfluenceSnapshotUseCase _captureInfluenceSnapshotUseCase;

    public SocialGraphController(
        UpsertSocialEdgeUseCase upsertSocialEdgeUseCase,
        CaptureInfluenceSnapshotUseCase captureInfluenceSnapshotUseCase)
    {
        _upsertSocialEdgeUseCase = upsertSocialEdgeUseCase;
        _captureInfluenceSnapshotUseCase = captureInfluenceSnapshotUseCase;
    }

    [HttpPost("edges")]
    public async Task<ActionResult<SocialEdgeResponse>> UpsertEdge([FromBody] UpsertSocialEdgeRequest request, CancellationToken ct)
    {
        var response = await _upsertSocialEdgeUseCase.ExecuteAsync(request, ct);
        return Ok(response);
    }

    [HttpPost("users/{userId}/snapshot")]
    public async Task<ActionResult<InfluenceSnapshotResponse>> CaptureSnapshot([FromRoute] string userId, CancellationToken ct)
    {
        var response = await _captureInfluenceSnapshotUseCase.ExecuteAsync(userId, ct);
        return Ok(response);
    }
}
