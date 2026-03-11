using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sovereign.Application.DTOs;
using Sovereign.Application.UseCases;

namespace Sovereign.API.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly GetDashboardOverviewUseCase _getDashboardOverviewUseCase;

    public DashboardController(GetDashboardOverviewUseCase getDashboardOverviewUseCase)
    {
        _getDashboardOverviewUseCase = getDashboardOverviewUseCase;
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<DashboardOverviewResponse>> GetOverview([FromRoute] string userId, CancellationToken ct)
    {
        var response = await _getDashboardOverviewUseCase.ExecuteAsync(userId, ct);
        return Ok(response);
    }
}
