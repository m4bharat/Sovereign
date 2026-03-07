using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sovereign.Intelligence.Clients;
using Sovereign.Intelligence.Configuration;
using Sovereign.Intelligence.Engines;
using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Parsers;
using Sovereign.Intelligence.Prompts;
using Sovereign.Intelligence.Services;

namespace Sovereign.Intelligence.DependencyInjection;

public static class IntelligenceServiceCollection
{
    public static IServiceCollection AddSovereignIntelligence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IPromptTemplateProvider, SocialPromptTemplateProvider>();
        services.AddSingleton<IRelationshipIntelligenceEngine, RelationshipIntelligenceEngine>();
        services.AddSingleton<IInteractionEngine, InteractionEngine>();
        services.AddSingleton<IAIStrategyService, AIStrategyService>();

        services.Configure<LlmOptions>(configuration.GetSection("Llm"));
        services.AddSingleton<AiDecisionPromptBuilder>();
        services.AddSingleton<AiDecisionJsonParser>();
        services.AddScoped<IAiDecisionService, AiDecisionService>();

        var provider = configuration.GetSection("Llm")["Provider"] ?? "OpenAI";
        if (string.Equals(provider, "Local", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<ILlmClient, LocalLlmClient>();
        else
            services.AddHttpClient<ILlmClient, OpenAiLlmClient>();

        return services;
    }
}
