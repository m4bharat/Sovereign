namespace Sovereign.Intelligence.Clients;

public interface ILlmClient
{
    Task<string> CompleteAsync(string prompt, CancellationToken ct = default);
    IAsyncEnumerable<string> StreamCompletionAsync(string prompt, CancellationToken ct = default);
}
