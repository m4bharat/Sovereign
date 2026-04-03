namespace Sovereign.Intelligence.Interfaces;

/// <summary>
/// Provides methods for building prompt templates for various scenarios.
/// </summary>
public interface IPromptTemplateProvider
{
    /// <summary>
    /// Builds a prompt for reconnecting based on the given parameters.
    /// </summary>
    /// <param name="role">The role of the user in the conversation.</param>
    /// <param name="lastTopicSummary">A summary of the last topic discussed.</param>
    /// <param name="stance">The stance to adopt in the prompt.</param>
    /// <returns>A string representing the reconnect prompt.</returns>
    string BuildReconnectPrompt(string role, string lastTopicSummary, string stance);

    /// <summary>
    /// Builds a prompt for maintaining a relationship based on the given parameters.
    /// </summary>
    /// <param name="role">The role of the user in the conversation.</param>
    /// <param name="lastTopicSummary">A summary of the last topic discussed.</param>
    /// <param name="stance">The stance to adopt in the prompt.</param>
    /// <returns>A string representing the maintain prompt.</returns>
    string BuildMaintainPrompt(string role, string lastTopicSummary, string stance);
}
