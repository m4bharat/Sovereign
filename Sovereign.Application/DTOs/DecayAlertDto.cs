namespace Sovereign.Application.DTOs;

public sealed class DecayAlertDto
{
    public Guid RelationshipId { get; init; }
    public string ContactId { get; init; } = string.Empty;
    public int DaysSilent { get; init; }
    public double DecayScore { get; init; }
    public string Temperature { get; init; } = string.Empty;
    public string SuggestedAction { get; init; } = string.Empty;
    public string SuggestedMessage { get; init; } = string.Empty;
}
