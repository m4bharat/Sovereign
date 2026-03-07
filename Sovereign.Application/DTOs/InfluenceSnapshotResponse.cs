namespace Sovereign.Application.DTOs;

public sealed class InfluenceSnapshotResponse
{
    public string UserId { get; init; } = string.Empty;
    public double AggregateInfluenceScore { get; init; }
    public DateTime CapturedAtUtc { get; init; }
}
