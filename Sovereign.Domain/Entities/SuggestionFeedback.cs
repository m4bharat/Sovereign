namespace Sovereign.Domain.Entities;

public sealed class SuggestionFeedback
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SuggestionId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int? Rating { get; set; }
    public string? FeedbackType { get; set; }
    public string? FeedbackText { get; set; }
    public bool? WasUseful { get; set; }
    public bool? WasGeneric { get; set; }
    public bool? WasWrongContext { get; set; }
    public bool? WasWrongTone { get; set; }
    public bool? WasTooLong { get; set; }
    public bool? WasTooShort { get; set; }
    public bool? Hallucinated { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
