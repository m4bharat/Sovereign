namespace Sovereign.Intelligence.Interfaces;

using Sovereign.Intelligence.Models;

public interface ISocialMovePlanner
{
    IReadOnlyList<SocialMoveCandidate> Plan(SocialSituation situation, RelationshipAnalysis relationshipAnalysis);
}
