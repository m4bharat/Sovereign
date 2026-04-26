namespace Sovereign.Domain.Entities;

public sealed class SuggestionEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string? TenantId { get; set; }
    public string? SessionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset EventTime { get; set; } = DateTimeOffset.UtcNow;
    public string? Platform { get; set; }
    public string? Surface { get; set; }
    public string? CurrentUrl { get; set; }
    public Guid? SuggestionId { get; set; }
    public Guid? RequestId { get; set; }
    public string? SituationType { get; set; }
    public string? Move { get; set; }
    public string? Strategy { get; set; }
    public string? Tone { get; set; }
    public double? Confidence { get; set; }
    public string? SourceAuthor { get; set; }
    public string? SourceTitle { get; set; }
    public string? SourceTextHash { get; set; }
    public string? InputMessageHash { get; set; }
    public string? ReplyHash { get; set; }
    public int? ReplyLength { get; set; }
    public int? EditedReplyLength { get; set; }
    public int? EditDistance { get; set; }
    public double? EditRatio { get; set; }
    public int? LatencyMs { get; set; }
    public string? ModelProvider { get; set; }
    public string? ModelName { get; set; }
    public bool? Accepted { get; set; }
    public bool? Posted { get; set; }
    public bool? Regenerated { get; set; }
    public string? MetadataJson { get; set; }
}
