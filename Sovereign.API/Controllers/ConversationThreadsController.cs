using Microsoft.AspNetCore.Mvc;
using Sovereign.Application.DTOs;
using Sovereign.Application.UseCases;

namespace Sovereign.API.Controllers;

[ApiController]
[Route("api/conversations")]
public sealed class ConversationThreadsController : ControllerBase
{
    private readonly CreateThreadUseCase _createThreadUseCase;
    private readonly AddMessageToThreadUseCase _addMessageToThreadUseCase;
    private readonly GenerateThreadSummaryUseCase _generateThreadSummaryUseCase;

    public ConversationThreadsController(
        CreateThreadUseCase createThreadUseCase,
        AddMessageToThreadUseCase addMessageToThreadUseCase,
        GenerateThreadSummaryUseCase generateThreadSummaryUseCase)
    {
        _createThreadUseCase = createThreadUseCase;
        _addMessageToThreadUseCase = addMessageToThreadUseCase;
        _generateThreadSummaryUseCase = generateThreadSummaryUseCase;
    }

    [HttpPost("threads")]
    public async Task<ActionResult<CreateThreadResponse>> CreateThread([FromBody] CreateThreadRequest request, CancellationToken ct)
    {
        var response = await _createThreadUseCase.ExecuteAsync(request, ct);
        return CreatedAtAction(nameof(CreateThread), new { id = response.ThreadId }, response);
    }

    [HttpPost("threads/{threadId:guid}/messages")]
    public async Task<ActionResult<AddMessageResponse>> AddMessage([FromRoute] Guid threadId, [FromBody] AddMessageRequest request, CancellationToken ct)
    {
        var response = await _addMessageToThreadUseCase.ExecuteAsync(new AddMessageRequest
        {
            ThreadId = threadId,
            SenderType = request.SenderType,
            Content = request.Content
        }, ct);

        return Ok(response);
    }

    [HttpPost("threads/{threadId:guid}/summary")]
    public async Task<ActionResult<GenerateThreadSummaryResponse>> GenerateSummary([FromRoute] Guid threadId, CancellationToken ct)
    {
        var response = await _generateThreadSummaryUseCase.ExecuteAsync(threadId, ct);
        return Ok(response);
    }

    [HttpGet]
    public IActionResult GetThreadsPlaceholder()
    {
        return Ok(new[] { "threads endpoint ready" });
    }
}
