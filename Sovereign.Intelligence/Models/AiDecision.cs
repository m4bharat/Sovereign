namespace Sovereign.Intelligence.Models;

public sealed class AiDecision
{
    public const string ReplyAction = "reply";
    public const string SaveMemoryAction = "save_memory";
    public const string SummarizeAction = "summarize";
    public const string NoAction = "no_action";

    public string Action { get; init; } = NoAction;
    public string Reply { get; init; } = string.Empty;
    public string MemoryKey { get; init; } = string.Empty;
    public string MemoryValue { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public double Confidence { get; init; }

    public static IReadOnlySet<string> AllowedActions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ReplyAction, SaveMemoryAction, SummarizeAction, NoAction
    };
}
