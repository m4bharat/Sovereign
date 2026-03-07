namespace Sovereign.Application.DTOs;

public sealed class LogInteractionResponse
{
    public Guid RelationshipId { get; init; }
    public DateTime LoggedAtUtc { get; init; }
}
