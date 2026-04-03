namespace Sovereign.Intelligence.Interfaces;

using Sovereign.Intelligence.Models;

public interface ICandidateScoringEngine
{
    IReadOnlyList<CandidateScore> Score(IReadOnlyList<SocialMoveCandidate> candidates, SocialSituation situation, MessageContext context, RelationshipAnalysis relationshipAnalysis);
}
