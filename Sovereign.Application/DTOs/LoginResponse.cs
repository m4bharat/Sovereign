namespace Sovereign.Application.DTOs;

public sealed class LoginResponse
{
    public string Token { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
}
