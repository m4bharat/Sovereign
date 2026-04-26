using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sovereign.Application.Interfaces;
using Sovereign.Infrastructure.Events;
using Sovereign.Infrastructure.Persistence;
using Sovereign.Infrastructure.Repositories;

namespace Sovereign.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<SovereignDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IRelationshipRepository, RelationshipRepository>();
        services.AddScoped<IMemoryRepository, MemoryRepository>();
        services.AddScoped<IConversationThreadRepository, ConversationThreadRepository>();
        services.AddScoped<IConversationMessageRepository, ConversationMessageRepository>();
        services.AddScoped<IConversationSummaryRepository, ConversationSummaryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISocialEdgeRepository, SocialEdgeRepository>();
        services.AddScoped<IInfluenceSnapshotRepository, InfluenceSnapshotRepository>();
        services.AddScoped<ITelemetryRepository, TelemetryRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        return services;
    }
}
