using Sovereign.Domain.Models;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Interfaces;

public interface ICandidateScoringEngine
{
    IReadOnlyList<CandidateScore> Score(IReadOnlyList<SocialMoveCandidate> candidates, SocialSituation situation, MessageContext context, RelationshipAnalysis relationshipAnalysis);
}
