using Sovereign.Application.DTOs;
using Sovereign.Application.Interfaces;
using Sovereign.Domain.Aggregates;

namespace Sovereign.Application.UseCases;

public class CreateRelationshipUseCase
{
    private readonly IRelationshipRepository _repository;

    public CreateRelationshipUseCase(IRelationshipRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> ExecuteAsync(CreateRelationshipRequest request)
    {
        var relationship = new Relationship
        {
            //Id = Guid.NewGuid(),
            ContactId = request.ContactId,
            UserId = request.UserId
        };

        await _repository.AddAsync(relationship);

        return relationship.Id;
    }
}