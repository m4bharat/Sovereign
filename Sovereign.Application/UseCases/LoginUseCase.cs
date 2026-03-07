using Sovereign.Application.DTOs;

namespace Sovereign.Application.UseCases;

public sealed class LoginUseCase
{
    public Task<LoginResponse> ExecuteAsync(LoginRequest request, CancellationToken ct = default)
    {
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        return Task.FromResult(new LoginResponse
        {
            Token = token,
            Email = request.Email,
            TenantId = request.TenantId
        });
    }
}
