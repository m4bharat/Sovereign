namespace Sovereign.Intelligence.Models;

public sealed class SocialSituation
{
    public string Type { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<string> Signals { get; init; } = Array.Empty<string>();
}
