
using System;
using Sovereign.Domain.Common;

namespace Sovereign.Domain.Events;

public sealed class RelationshipCreatedEvent : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
    public Guid RelationshipId { get; }

    public RelationshipCreatedEvent(Guid relationshipId)
    {
        RelationshipId = relationshipId;
    }
}
