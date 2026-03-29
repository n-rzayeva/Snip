using Confluent.Kafka;
using System.Text.Json;

namespace Snip.NotificationWorker.Services;

public class KafkaProducerService : IDisposable
{
    private readonly IProducer<string, string> _producer;

    public KafkaProducerService(IConfiguration configuration)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = configuration.GetConnectionString("Kafka")
        };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<T>(string topic, string key, T message)
    {
        var json = JsonSerializer.Serialize(message);
        await _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = key,
            Value = json
        });
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}