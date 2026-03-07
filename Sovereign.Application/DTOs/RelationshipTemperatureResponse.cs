namespace Sovereign.Application.DTOs;

public sealed class RelationshipTemperatureResponse
{
    public Guid RelationshipId { get; init; }
    public double Score { get; init; }
    public int SilenceDays { get; init; }
    public string Temperature { get; init; } = string.Empty;
    public string RecommendedAction { get; init; } = string.Empty;
}
