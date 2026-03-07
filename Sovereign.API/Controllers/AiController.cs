using Microsoft.AspNetCore.Mvc;
using Sovereign.Application.DTOs;
using Sovereign.Application.UseCases;

namespace Sovereign.API.Controllers;

[ApiController]
[Route("api/ai")]
public sealed class AiController : ControllerBase
{
    private readonly ProcessAiMessageUseCase _useCase;

    public AiController(ProcessAiMessageUseCase useCase)
    {
        _useCase = useCase;
    }

    [HttpPost("decide")]
    public async Task<ActionResult<AiDecisionResponse>> Decide([FromBody] AiDecisionRequest request, CancellationToken ct)
    {
        var response = await _useCase.ExecuteAsync(request, ct);
        return Ok(response);
    }
}
