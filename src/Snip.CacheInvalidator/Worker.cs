using Snip.CacheInvalidator.Services;

namespace Snip.CacheInvalidator;

public class Worker : BackgroundService
{
    private readonly CacheInvalidatorService _invalidator;
    private readonly ILogger<Worker> _logger;

    public Worker(CacheInvalidatorService invalidator, ILogger<Worker> logger)
    {
        _invalidator = invalidator;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cache Invalidator started");
        await _invalidator.ConsumeAsync(stoppingToken);
    }
}
