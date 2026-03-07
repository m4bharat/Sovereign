using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Interfaces;

public interface IInteractionEngine
{
    InteractionSuggestion GenerateSuggestion(RelationshipContext context, SocialInsight insight);
}
