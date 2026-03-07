namespace Sovereign.Domain.Entities;

public sealed class SocialEdge
{
    private SocialEdge()
    {
        SourceUserId = string.Empty;
        TargetContactId = string.Empty;
    }

    public SocialEdge(Guid id, string sourceUserId, string targetContactId)
    {
        if (string.IsNullOrWhiteSpace(sourceUserId))
            throw new ArgumentException("SourceUserId is required.", nameof(sourceUserId));
        if (string.IsNullOrWhiteSpace(targetContactId))
            throw new ArgumentException("TargetContactId is required.", nameof(targetContactId));

        Id = id;
        SourceUserId = sourceUserId;
        TargetContactId = targetContactId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string SourceUserId { get; private set; }
    public string TargetContactId { get; private set; }
    public double StrengthScore { get; private set; }
    public double InfluenceScore { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public void UpdateScores(double strengthScore, double influenceScore)
    {
        StrengthScore = strengthScore;
        InfluenceScore = influenceScore;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
