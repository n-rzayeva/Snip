using ClickHouse.Client.ADO;
using Confluent.Kafka;
using System.Text.Json;
using Snip.Shared;
using Snip.Shared.Events;

namespace Snip.NotificationWorker.Services;

public class NotificationService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly KafkaProducerService _producer;
    private readonly string _clickHouseConnectionString;
    private readonly ILogger<NotificationService> _logger;
    private readonly Dictionary<string, long> _clickCounts = new();

    public NotificationService(
        IConfiguration configuration,
        KafkaProducerService producer,
        ILogger<NotificationService> logger)
    {
        _logger = logger;
        _producer = producer;
        _clickHouseConnectionString = configuration.GetConnectionString("ClickHouse")!;

        var config = new ConsumerConfig
        {
            BootstrapServers = configuration.GetConnectionString("Kafka"),
            GroupId = "notification-worker",
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = true
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _consumer.Subscribe(Topics.ClickEvents);
    }

    public async Task InitializeCountsAsync()
    {
        try
        {
            using var connection = new ClickHouseConnection(_clickHouseConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT slug, count() FROM click_events GROUP BY slug";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var slug = reader.GetString(0);
                var total = Convert.ToInt64(reader.GetValue(1));
                _clickCounts[slug] = total;
                _logger.LogInformation("Initialized count for slug {Slug}: {Total}", slug, total);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize counts from ClickHouse");
        }
    }

    public async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(cancellationToken);
                if (result?.Message?.Value is null) continue;

                var clickEvent = JsonSerializer.Deserialize<ClickEvent>(result.Message.Value);
                if (clickEvent is null) continue;

                _clickCounts.TryGetValue(clickEvent.Slug, out var previous);
                var current = previous + 1;
                _clickCounts[clickEvent.Slug] = current;

                var milestone = ClickMilestones.GetCrossedMilestone(previous, current);
                if (milestone.HasValue)
                {
                    _logger.LogInformation(
                        "MILESTONE: Link {Slug} crossed {Milestone} clicks",
                        clickEvent.Slug, milestone.Value);

                    await _producer.PublishAsync(Topics.LinkAlerts, clickEvent.Slug, new LinkAlertEvent
                    {
                        Slug = clickEvent.Slug,
                        Milestone = milestone.Value,
                        RealTotal = current,
                        Timestamp = DateTime.UtcNow
                    });
                }

                _consumer.Commit(result);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in notification worker");
            }
        }

        _consumer.Close();
    }
}