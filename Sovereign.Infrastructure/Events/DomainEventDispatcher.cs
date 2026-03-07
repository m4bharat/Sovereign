
using Sovereign.Application.Interfaces;
using Sovereign.Domain.Common;

namespace Sovereign.Infrastructure.Events;

public class DomainEventDispatcher : IDomainEventDispatcher
{
    public Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        foreach (var domainEvent in events)
        {
            Console.WriteLine($"Dispatching domain event: {domainEvent.GetType().Name}");
        }

        return Task.CompletedTask;
    }
}
