using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Interfaces;

/// <summary>
/// Defines an engine for generating interaction suggestions based on context and insights.
/// </summary>
public interface IInteractionEngine
{
    /// <summary>
    /// Generates an interaction suggestion based on the provided context and social insight.
    /// </summary>
    /// <param name="context">The relationship context to analyze.</param>
    /// <param name="insight">The social insight to consider.</param>
    /// <returns>A suggestion for the next interaction.</returns>
    InteractionSuggestion GenerateSuggestion(RelationshipContext context, SocialInsight insight);
}
