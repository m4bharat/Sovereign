namespace Sovereign.Application.Services;

public sealed class SocialGraphScoringService
{
    public (double StrengthScore, double InfluenceScore) Calculate(
        int interactionCount,
        double reciprocityScore,
        double momentumScore,
        int silenceDays)
    {
        var strength = Math.Max(0d, interactionCount * 2d + reciprocityScore * 20d + momentumScore * 15d - silenceDays * 0.5d);
        var influence = Math.Max(0d, strength * 0.6d + reciprocityScore * 25d + momentumScore * 10d);
        return (Math.Round(strength, 2), Math.Round(influence, 2));
    }
}
