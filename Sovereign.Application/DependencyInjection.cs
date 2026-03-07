using Microsoft.Extensions.DependencyInjection;
using Sovereign.Application.Engines;
using Sovereign.Application.Interfaces;
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
        services.AddScoped<IToneAdjustmentStrategy, RuleBasedToneStrategy>();
        return services;
    }
}
