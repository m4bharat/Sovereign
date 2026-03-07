
using System;
using Sovereign.Domain.Common;

namespace Sovereign.Domain.Events;

public sealed class OutcomeRecordedEvent : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
    public Guid RelationshipId { get; }

    public OutcomeRecordedEvent(Guid relationshipId)
    {
        RelationshipId = relationshipId;
    }
}
