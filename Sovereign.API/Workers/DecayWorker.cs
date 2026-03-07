using Sovereign.Application.Services;

namespace Sovereign.API.Workers;

public sealed class DecayWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DecayWorker> _logger;

    public DecayWorker(IServiceProvider serviceProvider, ILogger<DecayWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<RelationshipDecayService>();
            await service.ScanAsync(stoppingToken);

            _logger.LogInformation("Decay scan cycle complete.");
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }
}
