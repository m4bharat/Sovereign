namespace Sovereign.Domain.Entities;

public sealed class SuggestionSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SuggestionId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? RequestPayloadJson { get; set; }
    public string? ResponsePayloadJson { get; set; }
    public string? SourceText { get; set; }
    public string? InputMessage { get; set; }
    public string? GeneratedReply { get; set; }
    public string? EditedReply { get; set; }
    public bool IsDebugSample { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
