using Sovereign.Application;
using Sovereign.Infrastructure;
using Sovereign.Intelligence.Clients;
using Sovereign.Intelligence.DependencyInjection;

namespace Sovereign.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSovereign(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

        services.AddApplication();
        services.AddInfrastructure(connectionString);
        services.AddSovereignIntelligence(configuration);
        //services.AddHttpClient<ILlmClient>()
        //                    .AddTransientHttpErrorPolicy(policy =>
        //                        policy.WaitAndRetryAsync(3, retry =>
        //                            TimeSpan.FromSeconds(Math.Pow(2, retry))));
        return services;
    }
}
