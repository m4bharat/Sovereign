namespace Sovereign.Intelligence.Clients;

using Sovereign.Intelligence.DecisionV2;

public interface ILlmClient
{
    Task<string> CompleteAsync(string prompt, CancellationToken ct = default);
    Task<DecisionV2Result> CompleteDecisionV2Async(string prompt, CancellationToken ct = default);
    IAsyncEnumerable<string> StreamCompletionAsync(string prompt, CancellationToken ct = default);
}
