
using Sovereign.Domain.Aggregates;

namespace Sovereign.Application.Engines;

public sealed class DecayScoringEngine
{
    public double Calculate(Relationship relationship)
    {
        var silenceDays = (DateTime.UtcNow - relationship.LastInteractionAtUtc).Days;

        double importanceWeight = 1.2;

        double decay =
            Math.Pow(silenceDays, 1.2)
            * importanceWeight
            * (1 + relationship.PowerDifferential)
            * (1 - relationship.ReciprocityScore);

        return decay;
    }
}
