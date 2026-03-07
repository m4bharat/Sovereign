using Sovereign.Application.DTOs;
using Sovereign.Application.Interfaces;

namespace Sovereign.Application.UseCases;

public sealed class LogInteractionUseCase
{
    private readonly IRelationshipRepository _repository;
    private readonly IDomainEventDispatcher _dispatcher;

    public LogInteractionUseCase(
        IRelationshipRepository repository,
        IDomainEventDispatcher dispatcher)
    {
        _repository = repository;
        _dispatcher = dispatcher;
    }

    public async Task<LogInteractionResponse> ExecuteAsync(Guid relationshipId, CancellationToken ct = default)
    {
        var relationship = await _repository.GetByIdAsync(relationshipId, ct)
            ?? throw new InvalidOperationException("Relationship not found.");

        relationship.LogInteraction();

        await _repository.UpdateAsync(relationship, ct);
        await _repository.SaveChangesAsync(ct);
        await _dispatcher.DispatchAsync(relationship.DomainEvents, ct);
        relationship.ClearDomainEvents();

        return new LogInteractionResponse
        {
            RelationshipId = relationship.Id,
            LoggedAtUtc = relationship.LastInteractionAtUtc
        };
    }
}
