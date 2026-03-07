namespace Sovereign.Application.DTOs;

public sealed class AddMessageResponse
{
    public Guid MessageId { get; init; }
    public Guid ThreadId { get; init; }
    public DateTime SentAtUtc { get; init; }
}
