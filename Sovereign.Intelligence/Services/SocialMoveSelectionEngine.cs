using System.Text.RegularExpressions;
using Sovereign.Domain.Models;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Services;

// This class is deprecated and should no longer be used.
// Use DecisionEngineV2 for all move selection logic instead.
[Obsolete("Use DecisionEngineV2 for all move selection logic.")]
public sealed class SocialMoveSelectionEngine
{
    public SocialMoveResult Select(MessageContext context)
    {
        throw new NotSupportedException("SocialMoveSelectionEngine is deprecated. Use DecisionEngineV2.");
    }
}

public sealed record SocialMoveResult(string Move, string Reply);
