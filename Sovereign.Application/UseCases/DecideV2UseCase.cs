using System.Threading;
using System.Threading.Tasks;
using Sovereign.Intelligence.DecisionV2;

namespace Sovereign.Application.UseCases;

public sealed class DecideV2UseCase
{
    private readonly IDecisionEngineV2 _decisionEngine;

    public DecideV2UseCase(IDecisionEngineV2 decisionEngine)
    {
        _decisionEngine = decisionEngine;
    }

    public Task<DecisionV2Result> ExecuteAsync(
        DecisionV2Input input,
        CancellationToken cancellationToken = default)
    {
        return _decisionEngine.DecideAsync(input, cancellationToken);
    }
}
