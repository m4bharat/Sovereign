namespace Sovereign.Domain.Entities;

public sealed class InfluenceSnapshot
{
    private InfluenceSnapshot()
    {
        UserId = string.Empty;
    }

    public InfluenceSnapshot(Guid id, string userId, double aggregateInfluenceScore)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required.", nameof(userId));

        Id = id;
        UserId = userId;
        AggregateInfluenceScore = aggregateInfluenceScore;
        CapturedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; }
    public double AggregateInfluenceScore { get; private set; }
    public DateTime CapturedAtUtc { get; private set; }
}
