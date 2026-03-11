using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sovereign.Application.DTOs;
using Sovereign.Application.UseCases;

namespace Sovereign.API.Controllers;

[ApiController]
[Authorize]
[Route("api/ai")]
public sealed class AiController : ControllerBase
{
    private readonly ProcessAiMessageUseCase _processAiMessageUseCase;
    private readonly RewriteMessageUseCase _rewriteMessageUseCase;

    public AiController(ProcessAiMessageUseCase processAiMessageUseCase, RewriteMessageUseCase rewriteMessageUseCase)
    {
        _processAiMessageUseCase = processAiMessageUseCase;
        _rewriteMessageUseCase = rewriteMessageUseCase;
    }

    [HttpPost("decide")]
    public async Task<ActionResult<AiDecisionResponse>> Decide([FromBody] AiDecisionRequest request, CancellationToken ct)
    {
        var response = await _processAiMessageUseCase.ExecuteAsync(request, ct);
        return Ok(response);
    }

    [HttpPost("rewrite")]
    public async Task<ActionResult<RewriteMessageResponse>> Rewrite([FromBody] RewriteMessageRequest request, CancellationToken ct)
    {
        var response = await _rewriteMessageUseCase.ExecuteAsync(request, ct);
        return Ok(response);
    }
}
