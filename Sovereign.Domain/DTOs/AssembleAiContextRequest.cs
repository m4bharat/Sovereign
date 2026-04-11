namespace Sovereign.Domain.DTOs;

public sealed class AssembleAiContextRequest
{
    public string UserId { get; init; } = string.Empty;
    public string ContactId { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string RelationshipRole { get; init; } = string.Empty;

    // Generic multi-platform context
    public string Platform { get; init; } = string.Empty;
    public string Surface { get; init; } = string.Empty;
    public string CurrentUrl { get; init; } = string.Empty;

    public string SourceAuthor { get; init; } = string.Empty;
    public string SourceTitle { get; init; } = string.Empty;
    public string SourceText { get; init; } = string.Empty;

    public string ParentContextText { get; init; } = string.Empty;
    public string NearbyContextText { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> InteractionMetadata { get; init; }
        = new Dictionary<string, string>();
}