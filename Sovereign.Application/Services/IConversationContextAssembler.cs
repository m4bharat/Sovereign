using Sovereign.Application.DTOs;
using Sovereign.Intelligence.Models;

namespace Sovereign.Application.Services;

public interface IConversationContextAssembler
{
    Task<MessageContext> AssembleAsync(AssembleAiContextRequest request, CancellationToken ct = default);
}
