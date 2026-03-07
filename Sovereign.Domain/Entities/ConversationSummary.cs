namespace Sovereign.Domain.Entities;

public sealed class ConversationSummary
{
    private ConversationSummary()
    {
        SummaryText = string.Empty;
    }

    public ConversationSummary(Guid id, Guid threadId, string summaryText)
    {
        if (threadId == Guid.Empty)
            throw new ArgumentException("ThreadId is required.", nameof(threadId));
        if (string.IsNullOrWhiteSpace(summaryText))
            throw new ArgumentException("SummaryText is required.", nameof(summaryText));

        Id = id;
        ThreadId = threadId;
        SummaryText = summaryText;
        GeneratedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid ThreadId { get; private set; }
    public string SummaryText { get; private set; }
    public DateTime GeneratedAtUtc { get; private set; }
}
