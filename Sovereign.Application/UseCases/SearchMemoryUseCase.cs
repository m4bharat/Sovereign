using Sovereign.Application.DTOs;
using Sovereign.Application.Interfaces;
using Sovereign.Application.Services;

namespace Sovereign.Application.UseCases;

public sealed class SearchMemoryUseCase
{
    private readonly IMemoryRepository _memoryRepository;
    private readonly MemorySimilarityService _similarityService;

    public SearchMemoryUseCase(IMemoryRepository memoryRepository, MemorySimilarityService similarityService)
    {
        _memoryRepository = memoryRepository;
        _similarityService = similarityService;
    }

    public async Task<MemorySearchResponse> ExecuteAsync(MemorySearchRequest request, CancellationToken ct = default)
    {
        var memories = await _memoryRepository.GetByUserIdAsync(request.UserId, ct);
        var results = _similarityService.Search(memories, request.Query, request.Limit)
            .Select(x => new MemorySearchResultDto
            {
                Id = x.Entry.Id,
                Key = x.Entry.Key,
                Value = x.Entry.Value,
                Score = x.Score,
                CreatedAtUtc = x.Entry.CreatedAtUtc
            })
            .ToList();

        return new MemorySearchResponse { Results = results };
    }
}
