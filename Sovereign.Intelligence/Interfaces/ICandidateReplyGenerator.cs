using Sovereign.Domain.Models;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Interfaces;

public interface ICandidateReplyGenerator
{
    IReadOnlyList<SocialMoveCandidate> Generate(IReadOnlyList<SocialMoveCandidate> moveCandidates, MessageContext context);
}
