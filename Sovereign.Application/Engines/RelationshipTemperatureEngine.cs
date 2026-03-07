using Sovereign.Domain.Aggregates;

namespace Sovereign.Application.Engines;

public sealed class RelationshipTemperatureEngine
{
    public (double Score, int SilenceDays, string Temperature, string RecommendedAction) Calculate(Relationship relationship)
    {
        var silenceDays = (DateTime.UtcNow - relationship.LastInteractionAtUtc).Days;

        var score =
            (relationship.ReciprocityScore * 35d)
            + (relationship.MomentumScore * 35d)
            + Math.Max(0d, 30d - silenceDays)
            - (Math.Max(0d, relationship.PowerDifferential) * 5d);

        score = Math.Clamp(Math.Round(score, 2), 0d, 100d);

        var temperature = score switch
        {
            >= 70d => "Hot",
            >= 40d => "Warm",
            _ => "Cold"
        };

        var action = temperature switch
        {
            "Hot" => "Maintain",
            "Warm" => "CheckIn",
            _ => "Reconnect"
        };

        return (score, silenceDays, temperature, action);
    }
}
