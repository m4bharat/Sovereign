using Sovereign.Application.DTOs;
using Sovereign.Application.Interfaces;
using Sovereign.Domain.Entities;

namespace Sovereign.Application.UseCases;

public sealed class RegisterUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public RegisterUseCase(IUserRepository userRepository, IPasswordHasher passwordHasher, ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<LoginResponse> ExecuteAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var tenantId = request.TenantId.Trim().ToLowerInvariant();

        var existing = await _userRepository.GetByEmailAsync(email, ct);
        if (existing is not null)
            throw new InvalidOperationException("A user with this email already exists.");

        var user = new UserAccount(Guid.NewGuid(), email, _passwordHasher.Hash(request.Password), tenantId);

        await _userRepository.AddAsync(user, ct);
        await _userRepository.SaveChangesAsync(ct);

        return new LoginResponse
        {
            Token = _tokenService.Create(user),
            UserId = user.Id,
            Email = user.Email,
            TenantId = user.TenantId
        };
    }
}
