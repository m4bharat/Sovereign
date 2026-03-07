namespace Sovereign.Application.DTOs;

public sealed class SocialEdgeResponse
{
    public string SourceUserId { get; init; } = string.Empty;
    public string TargetContactId { get; init; } = string.Empty;
    public double StrengthScore { get; init; }
    public double InfluenceScore { get; init; }
}
