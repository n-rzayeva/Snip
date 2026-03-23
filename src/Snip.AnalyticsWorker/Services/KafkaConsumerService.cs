using Confluent.Kafka;
using System.Text.Json;
using Snip.Shared.Events;
using Snip.Shared;

namespace Snip.AnalyticsWorker.Services;

public class KafkaConsumerService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<KafkaConsumerService> _logger;

    public KafkaConsumerService(IConfiguration configuration, ILogger<KafkaConsumerService> logger)
    {
        _logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = configuration.GetConnectionString("Kafka"),
            GroupId = "analytics-worker",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _consumer.Subscribe(Topics.ClickEvents);
    }

    public async Task ConsumeAsync(Func<ClickEvent, Task> handler, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(cancellationToken);

                if (result?.Message?.Value is null) continue;

                var clickEvent = JsonSerializer.Deserialize<ClickEvent>(result.Message.Value);

                if (clickEvent is null) continue;

                await handler(clickEvent);

                _consumer.Commit(result);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consuming click event");
            }
        }

        _consumer.Close();
    }
}