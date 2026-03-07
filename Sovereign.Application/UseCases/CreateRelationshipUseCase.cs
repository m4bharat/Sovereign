using Sovereign.Application.DTOs;
using Sovereign.Application.Interfaces;
using Sovereign.Domain.Aggregates;

namespace Sovereign.Application.UseCases;

public sealed class CreateRelationshipUseCase
{
    private readonly IRelationshipRepository _repository;
    private readonly IDomainEventDispatcher _dispatcher;

    public CreateRelationshipUseCase(
        IRelationshipRepository repository,
        IDomainEventDispatcher dispatcher)
    {
        _repository = repository;
        _dispatcher = dispatcher;
    }

    public async Task<CreateRelationshipResponse> ExecuteAsync(
        CreateRelationshipRequest request,
        CancellationToken ct = default)
    {
        var relationship = new Relationship(
            Guid.NewGuid(),
            request.UserId,
            request.ContactId,
            request.Role);

        await _repository.AddAsync(relationship, ct);
        await _repository.SaveChangesAsync(ct);
        await _dispatcher.DispatchAsync(relationship.DomainEvents, ct);
        relationship.ClearDomainEvents();

        return new CreateRelationshipResponse
        {
            RelationshipId = relationship.Id,
            UserId = relationship.UserId,
            ContactId = relationship.ContactId,
            Role = relationship.Role
        };
    }
}
