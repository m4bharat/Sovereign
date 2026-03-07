namespace Sovereign.Application.DTOs;

public sealed class DecayAlertsResponse
{
    public IReadOnlyList<DecayAlertDto> Alerts { get; init; } = Array.Empty<DecayAlertDto>();
}
