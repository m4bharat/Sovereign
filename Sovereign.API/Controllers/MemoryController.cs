using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sovereign.Application.DTOs;
using Sovereign.Application.Interfaces;
using Sovereign.Application.UseCases;

namespace Sovereign.API.Controllers;

[ApiController]
[Authorize]
[Route("api/memory")]
public sealed class MemoryController : ControllerBase
{
    private readonly IMemoryRepository _memoryRepository;
    private readonly SearchMemoryUseCase _searchMemoryUseCase;

    public MemoryController(IMemoryRepository memoryRepository, SearchMemoryUseCase searchMemoryUseCase)
    {
        _memoryRepository = memoryRepository;
        _searchMemoryUseCase = searchMemoryUseCase;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MemoryEntryDto>>> List([FromQuery] string userId, CancellationToken ct)
    {
        var memories = await _memoryRepository.GetByUserIdAsync(userId, ct);
        return Ok(memories.OrderByDescending(x => x.CreatedAtUtc).Select(x => new MemoryEntryDto
        {
            Id = x.Id,
            Key = x.Key,
            Value = x.Value,
            CreatedAtUtc = x.CreatedAtUtc
        }).ToList());
    }

    [HttpPost("search")]
    public async Task<ActionResult<MemorySearchResponse>> Search([FromBody] MemorySearchRequest request, CancellationToken ct)
    {
        var response = await _searchMemoryUseCase.ExecuteAsync(request, ct);
        return Ok(response);
    }
}
