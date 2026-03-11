namespace Sovereign.Application.DTOs;

public sealed class DashboardOverviewResponse
{
    public int RelationshipCount { get; init; }
    public int HotRelationships { get; init; }
    public int WarmRelationships { get; init; }
    public int ColdRelationships { get; init; }
    public int OpenDecayAlerts { get; init; }
    public int MemoryCount { get; init; }
    public IReadOnlyList<DecayAlertDto> PriorityAlerts { get; init; } = [];
    public IReadOnlyList<MemoryEntryDto> RecentMemories { get; init; } = [];
}
