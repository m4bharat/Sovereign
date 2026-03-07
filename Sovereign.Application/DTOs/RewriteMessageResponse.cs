namespace Sovereign.Application.DTOs;

public sealed class RewriteMessageResponse
{
    public IReadOnlyList<MessageRewriteVariantDto> Variants { get; init; } = Array.Empty<MessageRewriteVariantDto>();
}
