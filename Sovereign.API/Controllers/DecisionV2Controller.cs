using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Sovereign.Application.UseCases;
using Sovereign.Intelligence.DecisionV2;

namespace Sovereign.API.Controllers;

[ApiController]
[Route("api/ai/conversations")]
public sealed class DecisionV2Controller : ControllerBase
{
    private readonly DecideV2UseCase _useCase;

    public DecisionV2Controller(DecideV2UseCase useCase)
    {
        _useCase = useCase;
    }

    [HttpPost("decide-v2")]
    public async Task<IActionResult> DecideV2(
        [FromBody] DecisionV2Input input,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input.Message))
        {
            return BadRequest(new { error = "Message is required." });
        }

        var result = await _useCase.ExecuteAsync(input, cancellationToken);
        return Ok(result);
    }
}
