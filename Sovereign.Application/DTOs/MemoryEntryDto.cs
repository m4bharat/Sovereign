namespace Sovereign.Application.DTOs;

public sealed class MemoryEntryDto
{
    public Guid Id { get; init; }
    public string Key { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
}
