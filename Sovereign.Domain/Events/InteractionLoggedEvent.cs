
using System;
using Sovereign.Domain.Common;

namespace Sovereign.Domain.Events;

public sealed class InteractionLoggedEvent : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
    public Guid RelationshipId { get; }

    public InteractionLoggedEvent(Guid relationshipId)
    {
        RelationshipId = relationshipId;
    }
}
