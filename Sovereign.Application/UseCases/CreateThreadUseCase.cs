using Sovereign.Application.DTOs;
using Sovereign.Application.Interfaces;
using Sovereign.Domain.Entities;

namespace Sovereign.Application.UseCases;

public sealed class CreateThreadUseCase
{
    private readonly IConversationThreadRepository _threadRepository;

    public CreateThreadUseCase(IConversationThreadRepository threadRepository)
    {
        _threadRepository = threadRepository;
    }

    public async Task<CreateThreadResponse> ExecuteAsync(CreateThreadRequest request, CancellationToken ct = default)
    {
        var existing = await _threadRepository.GetByUserAndContactAsync(request.UserId, request.ContactId, ct);
        if (existing is not null)
        {
            return new CreateThreadResponse
            {
                ThreadId = existing.Id,
                UserId = existing.UserId,
                ContactId = existing.ContactId,
                Title = existing.Title
            };
        }

        var thread = new ConversationThread(Guid.NewGuid(), request.UserId, request.ContactId, request.Title);
        await _threadRepository.AddAsync(thread, ct);
        await _threadRepository.SaveChangesAsync(ct);

        return new CreateThreadResponse
        {
            ThreadId = thread.Id,
            UserId = thread.UserId,
            ContactId = thread.ContactId,
            Title = thread.Title
        };
    }
}
