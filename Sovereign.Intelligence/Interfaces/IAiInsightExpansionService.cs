using Sovereign.Domain.Models;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Interfaces;

public interface IAiInsightExpansionService
{
    Task<string?> GenerateInsightCommentAsync(
        MessageContext context,
        SocialMoveCandidate candidate,
        CancellationToken cancellationToken);
}
