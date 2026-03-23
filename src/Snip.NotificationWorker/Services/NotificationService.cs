using Confluent.Kafka;
using System.Text.Json;
using Snip.Shared;
using Snip.Shared.Events;

namespace Snip.NotificationWorker.Services;

public class NotificationService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<NotificationService> _logger;
    private readonly Dictionary<string, int> _clickCounts = new();
    private const int AlertThreshold = 10;

    public NotificationService(IConfiguration configuration, ILogger<NotificationService> logger)
    {
        _logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = configuration.GetConnectionString("Kafka"),
            GroupId = "notification-worker",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _consumer.Subscribe(Topics.ClickEvents);
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

                _clickCounts.TryGetValue(clickEvent.Slug, out var count);
                _clickCounts[clickEvent.Slug] = ++count;

                if (count % AlertThreshold == 0)
                {
                    _logger.LogInformation(
                        "ALERT: Link {Slug} has reached {Count} clicks",
                        clickEvent.Slug, count);
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