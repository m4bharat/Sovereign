namespace Sovereign.Application.DTOs;

public sealed class RewriteMessageRequest
{
    public string UserId { get; init; } = string.Empty;
    public string ContactId { get; init; } = string.Empty;
    public string Draft { get; init; } = string.Empty;
    public string RelationshipRole { get; init; } = string.Empty;
    public string Goal { get; init; } = string.Empty;
    public string Platform { get; init; } = string.Empty;
}
