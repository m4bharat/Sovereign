using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Services;

public interface IAiDecisionService
{
    Task<AiDecision> DecideAsync(MessageContext context, CancellationToken ct = default);
}
