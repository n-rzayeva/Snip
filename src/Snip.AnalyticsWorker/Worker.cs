using Snip.AnalyticsWorker.Services;

namespace Snip.AnalyticsWorker;

public class Worker : BackgroundService
{
    private readonly KafkaConsumerService _consumer;
    private readonly ClickHouseWriterService _writer;
    private readonly DashboardNotifier _notifier;
    private readonly ClickHouseMigrationRunner _migrationRunner;
    private readonly ILogger<Worker> _logger;

    public Worker(
        KafkaConsumerService consumer, 
        ClickHouseWriterService writer, 
        DashboardNotifier notifier,
        ClickHouseMigrationRunner migrationRunner,
        ILogger<Worker> logger)
    {
        _consumer = consumer;
        _writer = writer;
        _notifier = notifier;
        _migrationRunner = migrationRunner;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Analytics Worker started");
        
        await _migrationRunner.RunAsync();
        _logger.LogInformation("Migrations completed");

        await _consumer.ConsumeAsync(async clickEvent =>
        {
            await _writer.WriteClickEventAsync(clickEvent);
            
            var total = await _writer.GetTotalClicksAsync(clickEvent.Slug);
            await _notifier.NotifyClickAsync(clickEvent.Slug, total);

            _logger.LogInformation("Written click event for slug {Slug}", clickEvent.Slug);
        }, stoppingToken);
    }
}