using Sovereign.Domain.Common;

namespace Sovereign.Domain.Events;

public sealed class RelationshipCreatedEvent : IDomainEvent
{
    public RelationshipCreatedEvent(Guid relationshipId)
    {
        RelationshipId = relationshipId;
    }

    public Guid RelationshipId { get; }
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
