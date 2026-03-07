using Sovereign.Application.DTOs;
using Sovereign.Application.Interfaces;
using Sovereign.Application.Services;
using Sovereign.Domain.Entities;

namespace Sovereign.Application.UseCases;

public sealed class UpsertSocialEdgeUseCase
{
    private readonly ISocialEdgeRepository _edgeRepository;
    private readonly SocialGraphScoringService _scoringService;

    public UpsertSocialEdgeUseCase(
        ISocialEdgeRepository edgeRepository,
        SocialGraphScoringService scoringService)
    {
        _edgeRepository = edgeRepository;
        _scoringService = scoringService;
    }

    public async Task<SocialEdgeResponse> ExecuteAsync(UpsertSocialEdgeRequest request, CancellationToken ct = default)
    {
        var scores = _scoringService.Calculate(
            request.InteractionCount,
            request.ReciprocityScore,
            request.MomentumScore,
            request.SilenceDays);

        var edge = await _edgeRepository.GetAsync(request.SourceUserId, request.TargetContactId, ct);

        if (edge is null)
        {
            edge = new SocialEdge(Guid.NewGuid(), request.SourceUserId, request.TargetContactId);
            edge.UpdateScores(scores.StrengthScore, scores.InfluenceScore);
            await _edgeRepository.AddAsync(edge, ct);
        }
        else
        {
            edge.UpdateScores(scores.StrengthScore, scores.InfluenceScore);
        }

        await _edgeRepository.SaveChangesAsync(ct);

        return new SocialEdgeResponse
        {
            SourceUserId = edge.SourceUserId,
            TargetContactId = edge.TargetContactId,
            StrengthScore = edge.StrengthScore,
            InfluenceScore = edge.InfluenceScore
        };
    }
}
