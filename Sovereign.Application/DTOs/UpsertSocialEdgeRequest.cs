namespace Sovereign.Application.DTOs;

public sealed class UpsertSocialEdgeRequest
{
    public string SourceUserId { get; init; } = string.Empty;
    public string TargetContactId { get; init; } = string.Empty;
    public int InteractionCount { get; init; }
    public double ReciprocityScore { get; init; }
    public double MomentumScore { get; init; }
    public int SilenceDays { get; init; }
}
