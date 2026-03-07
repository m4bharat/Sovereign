using Sovereign.Intelligence.Interfaces;

namespace Sovereign.Intelligence.Prompts;

public sealed class SocialPromptTemplateProvider : IPromptTemplateProvider
{
    public string BuildReconnectPrompt(string role, string lastTopicSummary, string stance)
        => $"Write a {stance} reconnection message to a {role}. Last known topic: {lastTopicSummary}.";

    public string BuildMaintainPrompt(string role, string lastTopicSummary, string stance)
        => $"Write a {stance} maintenance message to a {role}. Last known topic: {lastTopicSummary}.";
}
