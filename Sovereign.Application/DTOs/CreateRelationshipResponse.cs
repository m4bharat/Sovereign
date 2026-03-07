using Sovereign.Domain.Enums;

namespace Sovereign.Application.DTOs;

public sealed class CreateRelationshipResponse
{
    public Guid RelationshipId { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string ContactId { get; init; } = string.Empty;
    public RelationshipRole Role { get; init; }
}
