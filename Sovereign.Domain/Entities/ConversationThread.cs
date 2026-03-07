namespace Sovereign.Domain.Entities;

public sealed class ConversationThread
{
    private ConversationThread()
    {
        UserId = string.Empty;
        ContactId = string.Empty;
        Title = string.Empty;
    }

    public ConversationThread(Guid id, string userId, string contactId, string title)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(contactId))
            throw new ArgumentException("ContactId is required.", nameof(contactId));

        Id = id;
        UserId = userId;
        ContactId = contactId;
        Title = title ?? string.Empty;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; }
    public string ContactId { get; private set; }
    public string Title { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
