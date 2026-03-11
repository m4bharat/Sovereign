namespace Sovereign.Application.DTOs;

public sealed class RecordOutcomeResponse
{
    public Guid RelationshipId { get; init; }
    public string OutcomeLabel { get; init; } = string.Empty;
    public DateTime RecordedAtUtc { get; init; }
}
