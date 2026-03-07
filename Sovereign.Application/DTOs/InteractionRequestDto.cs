
using Sovereign.Domain.Enums;

namespace Sovereign.Application.DTOs;

public sealed class InteractionRequestDto
{
    public Guid RelationshipId { get; init; }
    public MessageStance Stance { get; init; }
    public StrategicGoal Goal { get; init; }
}
