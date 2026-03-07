namespace Sovereign.Application.DTOs;

public sealed class AssembleAiContextRequest
{
    public string UserId { get; init; } = string.Empty;
    public string ContactId { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string RelationshipRole { get; init; } = string.Empty;
}
