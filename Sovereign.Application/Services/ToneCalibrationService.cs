using Sovereign.Application.DTOs;
using Sovereign.Application.Engines;
using Sovereign.Application.Interfaces;
using Sovereign.Domain.Enums;
using Sovereign.Domain.ValueObjects;
using Sovereign.Intelligence.Clients;

namespace Sovereign.Application.Services;

public sealed class ToneCalibrationService
{
    private static readonly string[] Stances = ["HighStatus", "WarmStrategic", "DirectEfficient"];

    private readonly IToneAdjustmentStrategy _toneAdjustmentStrategy;
    private readonly PromptComposer _promptComposer;
    private readonly ILlmClient _llmClient;

    public ToneCalibrationService(
        IToneAdjustmentStrategy toneAdjustmentStrategy,
        PromptComposer promptComposer,
        ILlmClient llmClient)
    {
        _toneAdjustmentStrategy = toneAdjustmentStrategy;
        _promptComposer = promptComposer;
        _llmClient = llmClient;
    }

    public async Task<RewriteMessageResponse> RewriteAsync(RewriteMessageRequest request, CancellationToken ct = default)
    {
        var role = ParseRole(request.RelationshipRole);
        var goal = ParseGoal(request.Goal);
        var platform = ParsePlatform(request.Platform);

        var variants = new List<MessageRewriteVariantDto>();

        foreach (var stance in Stances)
        {
            var baseVector = GetBaseVector(stance);
            var adjusted = _toneAdjustmentStrategy.Adjust(baseVector, role);

            var message = await GenerateVariantAsync(
                request.Draft,
                stance,
                role,
                goal,
                platform,
                adjusted,
                ct);

            variants.Add(new MessageRewriteVariantDto
            {
                Stance = stance,
                Message = message
            });
        }

        return new RewriteMessageResponse
        {
            Variants = variants
        };
    }

    private async Task<string> GenerateVariantAsync(
        string draft,
        string stance,
        RelationshipRole role,
        StrategicGoal goal,
        PlatformType platform,
        ToneVector tone,
        CancellationToken ct)
    {
        var prompt = _promptComposer.ComposeRewritePrompt(draft, stance, role, goal, platform, tone);

        try
        {
            var rewritten = await _llmClient.CompleteAsync(prompt, ct);

            if (!string.IsNullOrWhiteSpace(rewritten))
            {
                var trimmed = rewritten.Trim();
                if (trimmed.Length > 0 && trimmed.Length <= 1000)
                    return trimmed.Trim('"');
            }
        }
        catch
        {
        }

        return _promptComposer.BuildFallbackRewrite(draft, stance, role, goal);
    }

    private static ToneVector GetBaseVector(string stance)
        => stance switch
        {
            "HighStatus" => new ToneVector(0.35, 0.75, 0.70, 0.80, 0.25, 0.20),
            "WarmStrategic" => new ToneVector(0.80, 0.45, 0.55, 0.50, 0.35, 0.30),
            _ => new ToneVector(0.30, 0.85, 0.50, 0.90, 0.15, 0.15)
        };

    private static RelationshipRole ParseRole(string value)
        => Enum.TryParse<RelationshipRole>(value, true, out var role)
            ? role
            : RelationshipRole.Peer;

    private static StrategicGoal ParseGoal(string value)
        => Enum.TryParse<StrategicGoal>(value, true, out var goal)
            ? goal
            : StrategicGoal.Reconnect;

    private static PlatformType ParsePlatform(string value)
        => Enum.TryParse<PlatformType>(value, true, out var platform)
            ? platform
            : PlatformType.LinkedIn;
}
