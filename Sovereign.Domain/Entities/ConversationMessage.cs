namespace Sovereign.Domain.Entities;

public sealed class ConversationMessage
{
    private ConversationMessage()
    {
        SenderType = string.Empty;
        Content = string.Empty;
    }

    public ConversationMessage(Guid id, Guid threadId, string senderType, string content, DateTime? sentAtUtc = null)
    {
        if (threadId == Guid.Empty)
            throw new ArgumentException("ThreadId is required.", nameof(threadId));
        if (string.IsNullOrWhiteSpace(senderType))
            throw new ArgumentException("SenderType is required.", nameof(senderType));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content is required.", nameof(content));

        Id = id;
        ThreadId = threadId;
        SenderType = senderType;
        Content = content;
        SentAtUtc = sentAtUtc ?? DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid ThreadId { get; private set; }
    public string SenderType { get; private set; }
    public string Content { get; private set; }
    public DateTime SentAtUtc { get; private set; }
}
