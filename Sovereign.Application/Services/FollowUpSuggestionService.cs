using Sovereign.Domain.Aggregates;
using Sovereign.Domain.Enums;

namespace Sovereign.Application.Services;

public sealed class FollowUpSuggestionService
{
    public string BuildSuggestedMessage(Relationship relationship, string suggestedAction)
    {
        return relationship.Role switch
        {
            RelationshipRole.Investor or RelationshipRole.HiringManager
                => suggestedAction == "Reconnect"
                    ? "Quick check-in — wanted to reconnect and see how things are progressing on your side."
                    : "Wanted to follow up briefly and keep this thread warm.",
            RelationshipRole.Friend
                => suggestedAction == "Reconnect"
                    ? "Hey — it’s been a bit. Thought I’d check in and see how you’re doing."
                    : "Quick check-in — how have things been on your side?",
            _
                => suggestedAction == "Reconnect"
                    ? "Wanted to reconnect and continue the conversation when useful."
                    : "Quick follow-up to stay in touch and keep momentum going."
        };
    }
}
