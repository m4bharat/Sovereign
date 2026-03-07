using Sovereign.Application.DTOs;
using Sovereign.Application.Interfaces;
using Sovereign.Domain.Entities;

namespace Sovereign.Application.UseCases;

public sealed class AddMessageToThreadUseCase
{
    private readonly IConversationThreadRepository _threadRepository;
    private readonly IConversationMessageRepository _messageRepository;

    public AddMessageToThreadUseCase(
        IConversationThreadRepository threadRepository,
        IConversationMessageRepository messageRepository)
    {
        _threadRepository = threadRepository;
        _messageRepository = messageRepository;
    }

    public async Task<AddMessageResponse> ExecuteAsync(AddMessageRequest request, CancellationToken ct = default)
    {
        var thread = await _threadRepository.GetByIdAsync(request.ThreadId, ct)
            ?? throw new InvalidOperationException("Conversation thread not found.");

        var message = new ConversationMessage(Guid.NewGuid(), request.ThreadId, request.SenderType, request.Content);

        await _messageRepository.AddAsync(message, ct);
        thread.Touch();

        await _messageRepository.SaveChangesAsync(ct);
        await _threadRepository.SaveChangesAsync(ct);

        return new AddMessageResponse
        {
            MessageId = message.Id,
            ThreadId = message.ThreadId,
            SentAtUtc = message.SentAtUtc
        };
    }
}
