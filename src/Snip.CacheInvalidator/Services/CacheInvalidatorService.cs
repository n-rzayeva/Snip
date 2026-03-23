using Confluent.Kafka;
using StackExchange.Redis;
using System.Text.Json;
using Snip.Shared;
using Snip.Shared.Events;

namespace Snip.CacheInvalidator.Services;

public class CacheInvalidatorService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IDatabase _redis;
    private readonly ILogger<CacheInvalidatorService> _logger;

    public CacheInvalidatorService(IConfiguration configuration, ILogger<CacheInvalidatorService> logger)
    {
        _logger = logger;

        var kafkaConfig = new ConsumerConfig
        {
            BootstrapServers = configuration.GetConnectionString("Kafka"),
            GroupId = "cache-invalidator",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(kafkaConfig).Build();
        _consumer.Subscribe(new[] { Topics.LinkUpdated, Topics.LinkDeleted });

        var redis = ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")!);
        _redis = redis.GetDatabase();
    }

    public async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(cancellationToken);
                if (result?.Message?.Value is null) continue;

                var slug = result.Message.Key;

                await _redis.KeyDeleteAsync(slug);
                _logger.LogInformation("Evicted slug {Slug} from Redis cache", slug);

                _consumer.Commit(result);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in cache invalidator");
            }
        }

        _consumer.Close();
    }
}