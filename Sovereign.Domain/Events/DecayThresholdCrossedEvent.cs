
using System;
using Sovereign.Domain.Common;

namespace Sovereign.Domain.Events;

public sealed class DecayThresholdCrossedEvent : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
    public Guid RelationshipId { get; }
    public double DecayScore { get; }

    public DecayThresholdCrossedEvent(Guid relationshipId, double decayScore)
    {
        RelationshipId = relationshipId;
        DecayScore = decayScore;
    }
}
