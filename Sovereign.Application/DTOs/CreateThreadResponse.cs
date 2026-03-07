namespace Sovereign.Application.DTOs;

public sealed class CreateThreadResponse
{
    public Guid ThreadId { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string ContactId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
}
