namespace Sovereign.Application.DTOs;

public sealed class RegisterRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
}
