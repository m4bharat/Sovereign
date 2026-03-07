namespace Sovereign.Intelligence.Models;

public sealed class AiDecision
{
    public string Action { get; init; } = "no_action";
    public string Reply { get; init; } = string.Empty;
    public string MemoryKey { get; init; } = string.Empty;
    public string MemoryValue { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public double Confidence { get; init; }
}
