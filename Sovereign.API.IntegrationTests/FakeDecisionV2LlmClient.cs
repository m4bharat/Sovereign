using System.Threading;
using System.Threading.Tasks;
using Sovereign.Intelligence.Clients;
using Sovereign.Intelligence.DecisionV2;

namespace Sovereign.Intelligence.Tests;

public sealed class FakeDecisionV2LlmClient : ILlmClient
{
    public Task<string> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        // Return a fake legacy response for backward compatibility
        return Task.FromResult("{\"action\":\"reply\",\"memoryKey\":\"test\",\"memoryValue\":\"test\",\"summary\":\"Test response\"}");
    }

    public Task<DecisionV2Result> CompleteDecisionV2Async(string prompt, CancellationToken ct = default)
    {
        // Return a deterministic polished result for testing
        return Task.FromResult(new DecisionV2Result
        {
            Reply = "This is a polished, professional response that maintains the original intent while improving clarity and social intelligence.",
            Confidence = 0.95,
            Rationale = "Polished for better engagement and professionalism",
            Alternatives = new List<string>
            {
                "Here's an enhanced version that keeps the core message but refines the delivery.",
                "A more concise and impactful way to express the same sentiment."
            }
        });
    }

    public IAsyncEnumerable<string> StreamCompletionAsync(string prompt, CancellationToken ct = default)
    {
        throw new NotImplementedException("Streaming not implemented in fake client");
    }
}