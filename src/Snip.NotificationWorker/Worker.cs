using Snip.NotificationWorker.Services;

namespace Snip.NotificationWorker;

public class Worker : BackgroundService
{
    private readonly NotificationService _notification;
    private readonly ILogger<Worker> _logger;

    public Worker(NotificationService notification, ILogger<Worker> logger)
    {
        _notification = notification;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification Worker started");
        await _notification.ConsumeAsync(stoppingToken);
    }
}
