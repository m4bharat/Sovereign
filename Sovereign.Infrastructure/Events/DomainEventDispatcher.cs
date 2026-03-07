using Sovereign.Application.Interfaces;
using Sovereign.Domain.Common;

namespace Sovereign.Infrastructure.Events;

public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    public Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        foreach (var domainEvent in events)
        {
            Console.WriteLine($"Dispatched: {domainEvent.GetType().Name} at {domainEvent.OccurredOnUtc:O}");
        }

        return Task.CompletedTask;
    }
}
