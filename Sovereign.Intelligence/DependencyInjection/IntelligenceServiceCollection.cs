using Microsoft.Extensions.DependencyInjection;
using Sovereign.Intelligence.Engines;
using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Prompts;
using Sovereign.Intelligence.Services;

namespace Sovereign.Intelligence.DependencyInjection;

public static class IntelligenceServiceCollection
{
    public static IServiceCollection AddSovereignIntelligence(this IServiceCollection services)
    {
        services.AddSingleton<IPromptTemplateProvider, SocialPromptTemplateProvider>();
        services.AddSingleton<IRelationshipIntelligenceEngine, RelationshipIntelligenceEngine>();
        services.AddSingleton<IInteractionEngine, InteractionEngine>();
        services.AddSingleton<IAIStrategyService, AIStrategyService>();

        return services;
    }
}
