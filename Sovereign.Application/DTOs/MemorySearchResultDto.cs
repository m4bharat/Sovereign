namespace Sovereign.Application.DTOs;

public sealed class MemorySearchResultDto
{
    public Guid Id { get; init; }
    public string Key { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public double Score { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
