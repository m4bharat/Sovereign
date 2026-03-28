namespace Sovereign.Intelligence.Models;

public sealed class CandidateScore
{
    public SocialMoveCandidate Candidate { get; init; } = new();
    public double Relevance { get; init; }
    public double SocialFit { get; init; }
    public double Specificity { get; init; }
    public double HallucinationPenalty { get; init; }
    public double Tone { get; init; }
    public double Brevity { get; init; }

    public double Total =>
        (Relevance * 0.30) +
        (SocialFit * 0.25) +
        (Specificity * 0.20) +
        (Tone * 0.15) +
        (Brevity * 0.10) -
        HallucinationPenalty;
}
