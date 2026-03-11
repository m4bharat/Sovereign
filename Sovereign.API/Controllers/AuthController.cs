using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sovereign.Application.DTOs;
using Sovereign.Application.Interfaces;
using Sovereign.Application.UseCases;

namespace Sovereign.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly LoginUseCase _loginUseCase;
    private readonly RegisterUseCase _registerUseCase;
    private readonly IUserRepository _userRepository;

    public AuthController(LoginUseCase loginUseCase, RegisterUseCase registerUseCase, IUserRepository userRepository)
    {
        _loginUseCase = loginUseCase;
        _registerUseCase = registerUseCase;
        _userRepository = userRepository;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var response = await _registerUseCase.ExecuteAsync(request, ct);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var response = await _loginUseCase.ExecuteAsync(request, ct);
        return Ok(response);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthMeResponse>> Me(CancellationToken ct)
    {
        var sub = User.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub)?.Value
            ?? User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value;

        if (!Guid.TryParse(sub, out var userId))
            return Unauthorized();

        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return Unauthorized();

        return Ok(new AuthMeResponse
        {
            UserId = user.Id,
            Email = user.Email,
            TenantId = user.TenantId
        });
    }
}
