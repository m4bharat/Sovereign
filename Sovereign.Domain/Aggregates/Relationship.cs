
using System;
using System.Collections.Generic;
using Sovereign.Domain.Common;
using Sovereign.Domain.Enums;
using Sovereign.Domain.Events;

namespace Sovereign.Domain.Aggregates;

public class Relationship : BaseEntity
{
    private readonly List<DateTime> _interactionHistory = new();

    public required string UserId { get; set; }
    public required string ContactId { get; set; }
    public RelationshipRole Role { get; private set; }

    public double ReciprocityScore { get; private set; }
    public double MomentumScore { get; private set; }
    public double PowerDifferential { get; private set; }
    public double EmotionalTemperature { get; private set; }

    public DateTime LastInteractionAtUtc { get; private set; }

    public Relationship() { }

    public Relationship(Guid id, string userId, string contactId, RelationshipRole role)
    {
        Id = id;
        UserId = userId;
        ContactId = contactId;
        Role = role;
        LastInteractionAtUtc = DateTime.UtcNow;

        AddDomainEvent(new RelationshipCreatedEvent(id));
    }

    public void LogInteraction()
    {
        var now = DateTime.UtcNow;
        _interactionHistory.Add(now);
        LastInteractionAtUtc = now;
        MomentumScore += 0.1;

        AddDomainEvent(new InteractionLoggedEvent(Id));
    }

    public void RecordOutcome(OutcomeLabel label)
    {
        if (label == OutcomeLabel.PositiveResponse)
            ReciprocityScore += 0.05;

        AddDomainEvent(new OutcomeRecordedEvent(Id));
    }

    public double CalculateDecayScore()
    {
        var silenceDays = (DateTime.UtcNow - LastInteractionAtUtc).Days;

        return Math.Pow(silenceDays, 1.2)
               * (1 + PowerDifferential)
               * (1 - ReciprocityScore);
    }
}
