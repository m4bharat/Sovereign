using Sovereign.Domain.Common;

namespace Sovereign.Domain.Events;

public sealed class InteractionLoggedEvent : IDomainEvent
{
    public InteractionLoggedEvent(Guid relationshipId)
    {
        RelationshipId = relationshipId;
    }

    public Guid RelationshipId { get; }
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
