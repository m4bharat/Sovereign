using Sovereign.Domain.Enums;

namespace Sovereign.Intelligence.Models;

public sealed class RelationshipContext
{
    public Guid RelationshipId { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string ContactId { get; init; } = string.Empty;
    public RelationshipRole Role { get; init; }
    public int TotalInteractions { get; init; }
    public int SilenceDays { get; init; }
    public double ReciprocityScore { get; init; }
    public double MomentumScore { get; init; }
    public double PowerDifferential { get; init; }
    public double EmotionalTemperature { get; init; }
    public string LastTopicSummary { get; init; } = string.Empty;

    // New fields for richer relationship signals
    public string RecentRelationshipSummary { get; init; } = string.Empty;
    public double RelationshipStrengthScore { get; init; } = 0.0;
    public double RiskScore { get; init; } = 0.0;
    public double OpportunityScore { get; init; } = 0.0;
    public double ReplyUrgencyHint { get; init; } = 0.0;
}
