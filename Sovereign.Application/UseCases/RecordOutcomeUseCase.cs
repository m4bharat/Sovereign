using Sovereign.Application.DTOs;
using Sovereign.Application.Interfaces;
using Sovereign.Domain.Enums;

namespace Sovereign.Application.UseCases;

public sealed class RecordOutcomeUseCase
{
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly IDomainEventDispatcher _dispatcher;

    public RecordOutcomeUseCase(IRelationshipRepository relationshipRepository, IDomainEventDispatcher dispatcher)
    {
        _relationshipRepository = relationshipRepository;
        _dispatcher = dispatcher;
    }

    public async Task<RecordOutcomeResponse> ExecuteAsync(Guid relationshipId, RecordOutcomeRequest request, CancellationToken ct = default)
    {
        var relationship = await _relationshipRepository.GetByIdAsync(relationshipId, ct)
            ?? throw new InvalidOperationException("Relationship not found.");

        var label = Enum.TryParse<OutcomeLabel>(request.OutcomeLabel, true, out var parsed)
            ? parsed
            : throw new InvalidOperationException("Unsupported outcome label.");

        relationship.RecordOutcome(label);

        await _relationshipRepository.UpdateAsync(relationship, ct);
        await _relationshipRepository.SaveChangesAsync(ct);
        await _dispatcher.DispatchAsync(relationship.DomainEvents, ct);
        relationship.ClearDomainEvents();

        return new RecordOutcomeResponse
        {
            RelationshipId = relationship.Id,
            OutcomeLabel = label.ToString(),
            RecordedAtUtc = DateTime.UtcNow
        };
    }
}
