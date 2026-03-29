using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using Snip.LinkService.Hubs;
using Snip.Shared;
using Snip.Shared.Events;
using System.Text.Json;

namespace Snip.LinkService.Services;

public class ClickEventListener : BackgroundService
{
    private readonly IHubContext<ClickHub> _hubContext;
    private readonly ILogger<ClickEventListener> _logger;
    private readonly IConfiguration _configuration;

    public ClickEventListener(
        IConfiguration configuration,
        IHubContext<ClickHub> hubContext,
        ILogger<ClickEventListener> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Click event listener started");

        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration.GetConnectionString("Kafka"),
            GroupId = "link-service-signalr",
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(Topics.ClickEvents);

        await Task.Run(() =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    if (result?.Message?.Value is null) continue;

                    var clickEvent = JsonSerializer.Deserialize<ClickEvent>(result.Message.Value);
                    if (clickEvent is null) continue;

                    _hubContext.Clients.Group(clickEvent.Slug)
                        .SendAsync("ReceiveClickUpdate", clickEvent.Slug, stoppingToken);

                    _logger.LogInformation("SignalR nudge sent for slug {Slug}", clickEvent.Slug);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in click event listener");
                }
            }
            consumer.Close();
        }, stoppingToken);
    }
}