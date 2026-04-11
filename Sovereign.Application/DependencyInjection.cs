using Microsoft.Extensions.DependencyInjection;
using Sovereign.Application.Engines;
using Sovereign.Application.Interfaces;
using Sovereign.Application.Services;
using Sovereign.Application.UseCases;
using Sovereign.Domain.Services;

namespace Sovereign.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateRelationshipUseCase>();
        services.AddScoped<LogInteractionUseCase>();
        services.AddScoped<RecordOutcomeUseCase>();
        services.AddScoped<GenerateStrategyUseCase>();
        services.AddScoped<SearchMemoryUseCase>();
        services.AddScoped<GetDashboardOverviewUseCase>();
        services.AddScoped<CreateThreadUseCase>();
        services.AddScoped<AddMessageToThreadUseCase>();
        services.AddScoped<GenerateThreadSummaryUseCase>();
        services.AddScoped<LoginUseCase>();
        services.AddScoped<RegisterUseCase>();
        services.AddScoped<UpsertSocialEdgeUseCase>();
        services.AddScoped<CaptureInfluenceSnapshotUseCase>();
        services.AddScoped<RewriteMessageUseCase>();
        services.AddScoped<GetRelationshipTemperatureUseCase>();
        services.AddScoped<GetDecayAlertsUseCase>();
        services.AddScoped<IConversationContextAssembler, ConversationContextAssembler>();
        services.AddScoped<RelationshipDecayService>();
        services.AddScoped<SocialGraphScoringService>();
        services.AddScoped<FollowUpSuggestionService>();
        services.AddScoped<ToneCalibrationService>();
        services.AddScoped<MemorySimilarityService>();
        services.AddScoped<RelationshipTemperatureEngine>();
        services.AddScoped<DecayScoringEngine>();
        services.AddScoped<PromptComposer>();
        services.AddScoped<IToneAdjustmentStrategy, RuleBasedToneStrategy>();
        services.AddScoped<DecideV2UseCase>();
        return services;
    }
}
