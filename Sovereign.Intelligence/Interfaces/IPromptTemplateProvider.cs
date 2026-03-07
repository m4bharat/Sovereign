namespace Sovereign.Intelligence.Interfaces;

public interface IPromptTemplateProvider
{
    string BuildReconnectPrompt(string role, string lastTopicSummary, string stance);
    string BuildMaintainPrompt(string role, string lastTopicSummary, string stance);
}
