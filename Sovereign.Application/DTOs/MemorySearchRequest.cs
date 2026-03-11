namespace Sovereign.Application.DTOs;

public sealed class MemorySearchRequest
{
    public string UserId { get; init; } = string.Empty;
    public string Query { get; init; } = string.Empty;
    public int Limit { get; init; } = 5;
}
