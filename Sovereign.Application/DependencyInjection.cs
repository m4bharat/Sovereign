using Microsoft.Extensions.DependencyInjection;
using Sovereign.Application.Engines;
using Sovereign.Application.Interfaces;
using Sovereign.Application.Services;
using Sovereign.Application.UseCases;

namespace Sovereign.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateRelationshipUseCase>();
        services.AddScoped<LogInteractionUseCase>();
        services.AddScoped<GenerateStrategyUseCase>();
        services.AddScoped<ProcessAiMessageUseCase>();

        services.AddScoped<CreateThreadUseCase>();
        services.AddScoped<AddMessageToThreadUseCase>();
        services.AddScoped<GenerateThreadSummaryUseCase>();
        services.AddScoped<ProcessAiMessageWithContextUseCase>();
        services.AddScoped<LoginUseCase>();
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

        services.AddScoped<RelationshipTemperatureEngine>();
        services.AddScoped<DecayScoringEngine>();
        services.AddScoped<PromptComposer>();
        services.AddScoped<IToneAdjustmentStrategy, RuleBasedToneStrategy>();

        return services;
    }
}
