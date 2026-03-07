namespace Sovereign.Intelligence.Models;

public sealed class StrategyResult
{
    public SocialInsight Insight { get; init; } = new();
    public InteractionSuggestion Suggestion { get; init; } = new();
}
