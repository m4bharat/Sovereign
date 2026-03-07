namespace Sovereign.Domain.Entities;

/// <summary>
/// Represents a durable user memory extracted by the AI decision engine.
/// </summary>
public sealed class MemoryEntry
{
    private MemoryEntry()
    {
        UserId = string.Empty;
        Key = string.Empty;
        Value = string.Empty;
    }

    public MemoryEntry(Guid id, string userId, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key is required.", nameof(key));
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.", nameof(value));

        Id = id;
        UserId = userId;
        Key = key;
        Value = value;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string UserId { get; private set; }
    public string Key { get; private set; }
    public string Value { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
