namespace Sovereign.Application.DTOs;

public sealed class AddMessageRequest
{
    public Guid ThreadId { get; init; }
    public string SenderType { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
}
