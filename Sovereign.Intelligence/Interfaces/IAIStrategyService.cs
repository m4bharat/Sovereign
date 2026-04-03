using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Interfaces;

/// <summary>
/// Defines a service for generating strategies based on relationship context.
/// </summary>
public interface IAIStrategyService
{
    /// <summary>
    /// Generates a strategy based on the provided relationship context.
    /// </summary>
    /// <param name="context">The relationship context to analyze.</param>
    /// <returns>A result containing the generated strategy.</returns>
    StrategyResult GenerateStrategy(RelationshipContext context);
}
