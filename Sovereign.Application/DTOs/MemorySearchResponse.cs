namespace Sovereign.Application.DTOs;

public sealed class MemorySearchResponse
{
    public IReadOnlyList<MemorySearchResultDto> Results { get; init; } = [];
}
