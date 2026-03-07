using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Sovereign.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        // Example: register MediatR or validators later
        // services.AddMediatR(cfg =>
        //     cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        return services;
    }
}