namespace Sovereign.Intelligence.Clients;

public sealed class LocalLlmClient : ILlmClient
{
    public Task<string> CompleteAsync(string prompt, CancellationToken ct = default)
    {
        var normalized = prompt.ToLowerInvariant();

        if (normalized.Contains("birthday") || normalized.Contains("my name is") || normalized.Contains("i work at"))
            return Task.FromResult("""{"action":"save_memory","reply":"","memoryKey":"profile_fact","memoryValue":"Durable fact extracted from user message","summary":"","confidence":0.82}""");

        if (normalized.Contains("summarize") || normalized.Contains("summary") || normalized.Contains("recap"))
            return Task.FromResult("""{"action":"summarize","reply":"","memoryKey":"","memoryValue":"","summary":"Conversation summary requested. Local mode cannot generate a richer summary yet.","confidence":0.64}""");

        return Task.FromResult("""{"action":"reply","reply":"I understand. Tell me a little more and I’ll help you respond strategically.","memoryKey":"","memoryValue":"","summary":"","confidence":0.58}""");
    }
}
