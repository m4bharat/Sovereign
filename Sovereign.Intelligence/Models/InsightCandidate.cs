namespace Sovereign.Intelligence.Models;

public sealed class InsightCandidate
{
    public string Reply { get; init; } = string.Empty;
    public string Angle { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public double NoveltyScore { get; set; }
    public double GroundingScore { get; set; }
    public double PractitionerToneScore { get; set; }
    public double DerivativePenalty { get; set; }
    public double GenericPenalty { get; set; }
    public double RiskPenalty { get; set; }
    public double TotalScore { get; set; }
    public string[] Reasons { get; set; } = Array.Empty<string>();
}
