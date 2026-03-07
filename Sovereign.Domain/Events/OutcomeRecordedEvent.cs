using Sovereign.Domain.Common;

namespace Sovereign.Domain.Events;

public sealed class OutcomeRecordedEvent : IDomainEvent
{
    public OutcomeRecordedEvent(Guid relationshipId)
    {
        RelationshipId = relationshipId;
    }

    public Guid RelationshipId { get; }
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
