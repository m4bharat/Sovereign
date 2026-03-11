namespace Sovereign.Application.DTOs;

public sealed class AuthMeResponse
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
}
