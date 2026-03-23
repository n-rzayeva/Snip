using Snip.AnalyticsWorker.Services;

namespace Snip.AnalyticsWorker;

public class Worker : BackgroundService
{
    private readonly KafkaConsumerService _consumer;
    private readonly ClickHouseWriterService _writer;
    private readonly ILogger<Worker> _logger;

    public Worker(KafkaConsumerService consumer, ClickHouseWriterService writer, ILogger<Worker> logger)
    {
        _consumer = consumer;
        _writer = writer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Analytics Worker started");

        await _writer.EnsureTableExistsAsync();
        _logger.LogInformation("ClickHouse table verified");

        await _consumer.ConsumeAsync(async clickEvent =>
        {
            await _writer.WriteClickEventAsync(clickEvent);
            _logger.LogInformation("Written click event for slug {Slug}", clickEvent.Slug);
        }, stoppingToken);
    }
}