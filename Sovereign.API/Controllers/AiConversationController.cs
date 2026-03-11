using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sovereign.Application.DTOs;
using Sovereign.Application.UseCases;

namespace Sovereign.API.Controllers;

[ApiController]
[Authorize]
[Route("api/ai/conversations")]
public sealed class AiConversationController : ControllerBase
{
    private readonly ProcessAiMessageWithContextUseCase _useCase;

    public AiConversationController(ProcessAiMessageWithContextUseCase useCase)
    {
        _useCase = useCase;
    }

    [HttpPost("decide")]
    public async Task<ActionResult<AiDecisionResponse>> Decide([FromBody] AssembleAiContextRequest request, CancellationToken ct)
    {
        var response = await _useCase.ExecuteAsync(request, ct);
        return Ok(response);
    }
}
