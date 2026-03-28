namespace Sovereign.Intelligence.Models;

public sealed class MessageContext
{
    public string UserId { get; init; } = string.Empty;
    public string ContactId { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string RelationshipRole { get; init; } = string.Empty;

    // Existing repo-compatible fields
    public string RecentSummary { get; init; } = string.Empty;
    public string LastTopicSummary { get; init; } = string.Empty;
    public string RelevantMemories { get; init; } = string.Empty;

    // Existing Step 2 fields
    public string Platform { get; init; } = "LinkedIn";
    public IReadOnlyList<string> RecentMessages { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> MemoryFacts { get; init; } = Array.Empty<string>();

    // Generic multi-platform source context
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
}