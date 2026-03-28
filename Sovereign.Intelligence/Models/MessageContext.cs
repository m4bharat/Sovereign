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

    // Step 2 fields
    public string Platform { get; init; } = "LinkedIn";
    public IReadOnlyList<string> RecentMessages { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> MemoryFacts { get; init; } = Array.Empty<string>();
}