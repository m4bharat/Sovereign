using System.Threading;
using System.Threading.Tasks;

namespace Sovereign.Intelligence.DecisionV2;

public interface IDecisionEngineV2
{
    Task<DecisionV2Result> DecideAsync(
        DecisionV2Input input,
        CancellationToken cancellationToken = default);
}
