namespace Sovereign.Application.DTOs;

public sealed class GenerateThreadSummaryResponse
{
    public Guid ThreadId { get; init; }
    public string Summary { get; init; } = string.Empty;
}
