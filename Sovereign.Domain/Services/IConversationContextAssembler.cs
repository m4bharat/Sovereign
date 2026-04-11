using System.Threading;
using System.Threading.Tasks;
using Sovereign.Domain.DTOs;
using Sovereign.Domain.Models;

namespace Sovereign.Domain.Services;

public interface IConversationContextAssembler
{
    Task<MessageContext> AssembleAsync(
        AssembleAiContextRequest request,
        CancellationToken ct = default);
}