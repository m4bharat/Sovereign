using Sovereign.Application.DTOs;
using Sovereign.Application.Interfaces;

namespace Sovereign.Application.UseCases;

public sealed class LoginUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public LoginUseCase(IUserRepository userRepository, IPasswordHasher passwordHasher, ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<LoginResponse> ExecuteAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _userRepository.GetByEmailAsync(email, ct)
            ?? throw new InvalidOperationException("Invalid email or password.");

        if (!string.Equals(user.TenantId, request.TenantId.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid email or password.");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidOperationException("Invalid email or password.");

        return new LoginResponse
        {
            Token = _tokenService.Create(user),
            UserId = user.Id,
            Email = user.Email,
            TenantId = user.TenantId
        };
    }
}
