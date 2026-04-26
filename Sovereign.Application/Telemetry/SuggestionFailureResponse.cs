namespace Sovereign.Application.Telemetry;

public sealed class SuggestionFailureResponse
{
    public Guid SuggestionId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Surface { get; set; } = string.Empty;
    public string SituationType { get; set; } = string.Empty;
    public string Move { get; set; } = string.Empty;
    public string LatestEventType { get; set; } = string.Empty;
    public string FailureReason { get; set; } = string.Empty;
    public double EditRatio { get; set; }
    public bool Regenerated { get; set; }
    public bool Discarded { get; set; }
    public DateTimeOffset EventTime { get; set; }
}
