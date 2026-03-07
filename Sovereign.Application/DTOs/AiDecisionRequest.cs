namespace Sovereign.Application.DTOs;

public sealed class AiDecisionRequest
{
    public string UserId { get; init; } = string.Empty;
    public string ContactId { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string RelationshipRole { get; init; } = string.Empty;
    public string RecentSummary { get; init; } = string.Empty;
    public string LastTopicSummary { get; init; } = string.Empty;
}
