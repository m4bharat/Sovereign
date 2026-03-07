using Sovereign.Intelligence.Interfaces;

namespace Sovereign.Intelligence.Prompts;

public sealed class SocialPromptTemplateProvider : IPromptTemplateProvider
{
    public string BuildReconnectPrompt(string role, string lastTopicSummary, string stance)
    {
        return
            $"Write a {stance} reconnection message to a {role}. " +
            $"Reference prior context naturally. Last known topic: {lastTopicSummary}. " +
            "The message should feel human, concise, and strategically calibrated.";
    }

    public string BuildMaintainPrompt(string role, string lastTopicSummary, string stance)
    {
        return
            $"Write a {stance} relationship-maintenance message to a {role}. " +
            $"Use the prior conversation context when useful. Last known topic: {lastTopicSummary}. " +
            "The message should preserve warmth while respecting leverage.";
    }
}
