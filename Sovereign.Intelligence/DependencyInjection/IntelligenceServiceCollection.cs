using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sovereign.Intelligence.Clients;
using Sovereign.Intelligence.Configuration;
using Sovereign.Intelligence.DecisionV2;
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
        services.AddSingleton<AiDecisionPromptBuilder>();
        services.AddSingleton<AiDecisionJsonParser>();
        services.AddScoped<IAiDecisionService, AiDecisionService>();

        services.AddSingleton<DecisionV2PromptBuilder>();
        services.AddScoped<IDecisionEngineV2, DecisionEngineV2>();

        services.Configure<LlmOptions>(configuration.GetSection("Llm"));
        services.Configure<OpenAiOptions>(configuration.GetSection("OpenAI"));
        services.Configure<OllamaOptions>(configuration.GetSection("Ollama"));

        services.AddSingleton<LocalLlmClient>();
        services.AddHttpClient<OpenAiLlmClient>();
        services.AddHttpClient<OllamaLlmClient>();

        services.AddScoped<ILlmClient>(sp =>
        {
            var llmOptions = sp.GetRequiredService<IOptions<LlmOptions>>().Value;
            return llmOptions.Provider.ToLowerInvariant() switch
            {
                "local" => sp.GetRequiredService<LocalLlmClient>(),
                "ollama" => sp.GetRequiredService<OllamaLlmClient>(),
                "openai" => sp.GetRequiredService<OpenAiLlmClient>(),
                _ => throw new InvalidOperationException($"Unsupported LLM provider: {llmOptions.Provider}")
            };
        });

        return services;
    }
}
