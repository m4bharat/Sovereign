using Sovereign.Application.DTOs;
using Sovereign.Application.Interfaces;
using Sovereign.Domain.Entities;

namespace Sovereign.Application.UseCases;

public sealed class CaptureInfluenceSnapshotUseCase
{
    private readonly ISocialEdgeRepository _edgeRepository;
    private readonly IInfluenceSnapshotRepository _snapshotRepository;

    public CaptureInfluenceSnapshotUseCase(
        ISocialEdgeRepository edgeRepository,
        IInfluenceSnapshotRepository snapshotRepository)
    {
        _edgeRepository = edgeRepository;
        _snapshotRepository = snapshotRepository;
    }

    public async Task<InfluenceSnapshotResponse> ExecuteAsync(string userId, CancellationToken ct = default)
    {
        var edges = await _edgeRepository.GetByUserAsync(userId, ct);
        var aggregate = edges.Count == 0 ? 0d : edges.Average(x => x.InfluenceScore);

        var snapshot = new InfluenceSnapshot(Guid.NewGuid(), userId, Math.Round(aggregate, 2));
        await _snapshotRepository.AddAsync(snapshot, ct);
        await _snapshotRepository.SaveChangesAsync(ct);

        return new InfluenceSnapshotResponse
        {
            UserId = snapshot.UserId,
            AggregateInfluenceScore = snapshot.AggregateInfluenceScore,
            CapturedAtUtc = snapshot.CapturedAtUtc
        };
    }
}
