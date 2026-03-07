namespace Sovereign.Application.DTOs;

public sealed class AiDecisionResponse
{
    public string Action { get; init; } = string.Empty;
    public string Reply { get; init; } = string.Empty;
    public string MemoryKey { get; init; } = string.Empty;
    public string MemoryValue { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public double Confidence { get; init; }
}
