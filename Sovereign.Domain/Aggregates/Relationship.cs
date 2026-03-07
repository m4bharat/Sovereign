using Sovereign.Domain.Common;
using Sovereign.Domain.Enums;
using Sovereign.Domain.Events;

namespace Sovereign.Domain.Aggregates;

public class Relationship : BaseEntity
{
    private Relationship()
    {
        UserId = string.Empty;
        ContactId = string.Empty;
    }

    public Relationship(Guid id, string userId, string contactId, RelationshipRole role)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(contactId))
            throw new ArgumentException("ContactId is required.", nameof(contactId));

        Id = id;
        UserId = userId;
        ContactId = contactId;
        Role = role;
        LastInteractionAtUtc = DateTime.UtcNow;
        ReciprocityScore = 0.50d;
        MomentumScore = 0.0d;
        PowerDifferential = 0.0d;
        EmotionalTemperature = 0.0d;

        AddDomainEvent(new RelationshipCreatedEvent(id));
    }

    public string UserId { get; private set; }
    public string ContactId { get; private set; }
    public RelationshipRole Role { get; private set; }

    public double ReciprocityScore { get; private set; }
    public double MomentumScore { get; private set; }
    public double PowerDifferential { get; private set; }
    public double EmotionalTemperature { get; private set; }

    public DateTime LastInteractionAtUtc { get; private set; }

    public void LogInteraction()
    {
        LastInteractionAtUtc = DateTime.UtcNow;
        MomentumScore = Math.Min(1.0d, MomentumScore + 0.10d);
        AddDomainEvent(new InteractionLoggedEvent(Id));
    }

    public void RecordOutcome(OutcomeLabel label)
    {
        if (label == OutcomeLabel.PositiveResponse)
            ReciprocityScore = Math.Min(1.0d, ReciprocityScore + 0.05d);

        AddDomainEvent(new OutcomeRecordedEvent(Id));
    }

    public void UpdateSignals(double powerDifferential, double emotionalTemperature)
    {
        PowerDifferential = powerDifferential;
        EmotionalTemperature = emotionalTemperature;
    }
}
