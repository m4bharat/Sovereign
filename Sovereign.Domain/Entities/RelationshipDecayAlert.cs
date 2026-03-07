namespace Sovereign.Domain.Entities;

public sealed class RelationshipDecayAlert
{
    private RelationshipDecayAlert() { }

    public RelationshipDecayAlert(Guid id, Guid relationshipId, string message)
    {
        if (relationshipId == Guid.Empty)
            throw new ArgumentException("RelationshipId is required.", nameof(relationshipId));

        Id = id;
        RelationshipId = relationshipId;
        Message = message ?? string.Empty;
        TriggeredAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid RelationshipId { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public DateTime TriggeredAtUtc { get; private set; }
}
