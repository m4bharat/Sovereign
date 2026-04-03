namespace Sovereign.Intelligence.Interfaces;

using Sovereign.Intelligence.Models;

public interface ICandidateReplyGenerator
{
    IReadOnlyList<SocialMoveCandidate> Generate(IReadOnlyList<SocialMoveCandidate> moveCandidates, MessageContext context);
}
