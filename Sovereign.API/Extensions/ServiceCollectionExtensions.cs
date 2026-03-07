
using Sovereign.Application;
using Sovereign.Infrastructure;

namespace Sovereign.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSovereign(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddApplication();
        services.AddInfrastructure(connectionString);

        return services;
    }
}
