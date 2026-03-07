namespace Sovereign.Intelligence.Clients;

/// <summary>
/// Safe local fallback implementation. Replace with Ollama/vLLM/LM Studio integration.
/// </summary>
public sealed class LocalLlmClient : ILlmClient
{
    public Task<string> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        var normalized = prompt.ToLowerInvariant();

        if (normalized.Contains("remember") || normalized.Contains("birthday") || normalized.Contains("my name is"))
        {
            return Task.FromResult("{\"action\":\"save_memory\",\"reply\":\"Got it — I’ll remember that.\",\"memoryKey\":\"fact\",\"memoryValue\":\"Extracted user fact\",\"summary\":\"\",\"confidence\":0.72}");
        }

        if (normalized.Contains("summarize") || normalized.Contains("summary") || normalized.Contains("recap"))
        {
            return Task.FromResult("{\"action\":\"summarize\",\"reply\":\"\",\"memoryKey\":\"\",\"memoryValue\":\"\",\"summary\":\"Conversation summary not yet available in local mode.\",\"confidence\":0.61}");
        }

        return Task.FromResult("{\"action\":\"reply\",\"reply\":\"Understood.\",\"memoryKey\":\"\",\"memoryValue\":\"\",\"summary\":\"\",\"confidence\":0.55}");
    }
}
