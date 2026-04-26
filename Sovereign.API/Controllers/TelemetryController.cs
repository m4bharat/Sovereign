using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sovereign.Application.Telemetry;

namespace Sovereign.API.Controllers;

[ApiController]
[Authorize]
[Route("api/telemetry")]
public sealed class TelemetryController : ControllerBase
{
    private readonly ITelemetryService _telemetryService;

    public TelemetryController(ITelemetryService telemetryService)
    {
        _telemetryService = telemetryService;
    }

    [HttpPost("events")]
    public async Task<IActionResult> TrackEvent(
        [FromBody] TrackSuggestionEventRequest request,
        CancellationToken cancellationToken)
    {
        await _telemetryService.TrackEventAsync(request, cancellationToken);
        return Accepted();
    }

    [HttpPost("feedback")]
    public async Task<IActionResult> SubmitFeedback(
        [FromBody] SubmitSuggestionFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        await _telemetryService.SubmitFeedbackAsync(request, cancellationToken);
        return Accepted();
    }

    [HttpGet("analytics/summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await _telemetryService.GetSummaryAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("analytics/by-surface")]
    public async Task<IActionResult> GetBySurface(CancellationToken cancellationToken)
    {
        var result = await _telemetryService.GetBySurfaceAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("analytics/by-situation")]
    public async Task<IActionResult> GetBySituation(CancellationToken cancellationToken)
    {
        var result = await _telemetryService.GetBySituationAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("analytics/by-move")]
    public async Task<IActionResult> GetByMove(CancellationToken cancellationToken)
    {
        var result = await _telemetryService.GetByMoveAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("analytics/recent-failures")]
    public async Task<IActionResult> GetRecentFailures(CancellationToken cancellationToken)
    {
        var result = await _telemetryService.GetRecentFailuresAsync(cancellationToken);
        return Ok(result);
    }
}
