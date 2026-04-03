namespace Sovereign.Intelligence.Models;

public sealed class MessageContext
{
    public string UserId { get; init; } = string.Empty;
    public string ContactId { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string RelationshipRole { get; init; } = string.Empty;

    public string RecentSummary { get; init; } = string.Empty;
    public string LastTopicSummary { get; init; } = string.Empty;
    public string RelevantMemories { get; init; } = string.Empty;

    public string Platform { get; init; } = "LinkedIn";
    public IReadOnlyList<string> RecentMessages { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> MemoryFacts { get; init; } = Array.Empty<string>();

    public string Surface { get; init; } = string.Empty;
    public string CurrentUrl { get; init; } = string.Empty;

    public string SourceAuthor { get; init; } = string.Empty;
    public string SourceTitle { get; init; } = string.Empty;
    public string SourceText { get; init; } = string.Empty;

    public string ParentContextText { get; init; } = string.Empty;
    public string NearbyContextText { get; init; } = string.Empty;

    public string InteractionMode { get; init; } = "compose";

    public IReadOnlyDictionary<string, string> InteractionMetadata { get; init; }
        = new Dictionary<string, string>();

    // New fields for richer context
    public double RiskScore { get; init; } = 0.0;
    public double OpportunityScore { get; init; } = 0.0;
    public string SituationType { get; init; } = string.Empty;
    public string DesiredTone { get; init; } = string.Empty;
}