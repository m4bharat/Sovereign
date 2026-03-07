using Sovereign.Domain.Aggregates;
using Sovereign.Intelligence.Models;

namespace Sovereign.Application.Mappers;

public static class RelationshipContextMapper
{
    public static RelationshipContext Map(Relationship relationship)
    {
        var silenceDays = (DateTime.UtcNow - relationship.LastInteractionAtUtc).Days;

        return new RelationshipContext
        {
            RelationshipId = relationship.Id,
            UserId = relationship.UserId,
            ContactId = relationship.ContactId,
            Role = relationship.Role,
            TotalInteractions = (int)Math.Round(relationship.MomentumScore * 10),
            SilenceDays = silenceDays,
            ReciprocityScore = relationship.ReciprocityScore,
            MomentumScore = relationship.MomentumScore,
            PowerDifferential = relationship.PowerDifferential,
            EmotionalTemperature = relationship.EmotionalTemperature,
            LastTopicSummary = "No topic summary available yet."
        };
    }
}
