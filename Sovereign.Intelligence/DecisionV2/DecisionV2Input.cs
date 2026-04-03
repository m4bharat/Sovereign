namespace Sovereign.Intelligence.DecisionV2;

public sealed class DecisionV2Input
{
    public string UserId { get; set; } = string.Empty;
    public string ContactId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Platform { get; set; } = "linkedin";
    public string Surface { get; set; } = "post_compose";
    public string CurrentUrl { get; set; } = string.Empty;
    public string SourceAuthor { get; set; } = string.Empty;
    public string SourceTitle { get; set; } = string.Empty;
    public string SourceText { get; set; } = string.Empty;
    public string ParentContextText { get; set; } = string.Empty;
    public string NearbyContextText { get; set; } = string.Empty;
    public string RelationshipRole { get; set; } = "Peer";
    public int LastInteractionDays { get; set; } = 0;
    public int TotalInteractions { get; set; } = 0;
    public double ReciprocityScore { get; set; } = 0.0;
    public double MomentumScore { get; set; } = 0.0;
    public double PowerDifferential { get; set; } = 0.0;
    public double EmotionalTemperature { get; set; } = 0.0;
    public string RecentRelationshipSummary { get; set; } = string.Empty;
    public List<string> RelevantMemories { get; set; } = new();
    public bool AllowNoReply { get; set; } = true;
    public bool RequestAlternatives { get; set; } = false;
}
