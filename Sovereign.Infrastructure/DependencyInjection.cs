
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sovereign.Application.Interfaces;
using Sovereign.Infrastructure.Events;
using Sovereign.Infrastructure.Persistence;
using Sovereign.Infrastructure.Repositories;

namespace Sovereign.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<SovereignDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IRelationshipRepository, RelationshipRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        return services;
    }
}
