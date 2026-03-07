using Microsoft.Extensions.Logging;

namespace Sovereign.Application.Services;

public sealed class RelationshipDecayService
{
    private readonly ILogger<RelationshipDecayService> _logger;

    public RelationshipDecayService(ILogger<RelationshipDecayService> logger)
    {
        _logger = logger;
    }

    public Task ScanAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Scanning relationships for decay alerts.");
        return Task.CompletedTask;
    }
}
