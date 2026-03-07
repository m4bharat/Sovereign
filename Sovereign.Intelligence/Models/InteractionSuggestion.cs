namespace Sovereign.Intelligence.Models;

public sealed class InteractionSuggestion
{
    public string RecommendedAction { get; init; } = string.Empty;
    public string RecommendedStance { get; init; } = string.Empty;
    public string DraftPrompt { get; init; } = string.Empty;
    public string Reasoning { get; init; } = string.Empty;
}
