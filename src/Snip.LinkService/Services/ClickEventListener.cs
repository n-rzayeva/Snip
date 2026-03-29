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

        await Task.WhenAll(
            ConsumeClickEvents(stoppingToken),
            ConsumeAlerts(stoppingToken)
        );
    }

    private async Task ConsumeClickEvents(CancellationToken ct)
    {
        using var consumer = BuildConsumer("link-service-signalr");
        consumer.Subscribe(Topics.ClickEvents);

        await Task.Run(() =>
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(ct);
                    if (result?.Message?.Value is null) continue;

                    var clickEvent = JsonSerializer.Deserialize<ClickEvent>(result.Message.Value);
                    if (clickEvent is null) continue;

                    _hubContext.Clients.Group(clickEvent.Slug)
                        .SendAsync("ReceiveClickUpdate", clickEvent.Slug, ct);

                    _logger.LogInformation("SignalR nudge sent for slug {Slug}", clickEvent.Slug);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) { _logger.LogError(ex, "Error consuming click event"); }
            }
            consumer.Close();
        }, ct);
    }

    private async Task ConsumeAlerts(CancellationToken ct)
    {
        using var consumer = BuildConsumer("link-service-alerts");
        consumer.Subscribe(Topics.LinkAlerts);

        await Task.Run(() =>
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(ct);
                    if (result?.Message?.Value is null) continue;

                    var alert = JsonSerializer.Deserialize<LinkAlertEvent>(result.Message.Value);
                    if (alert is null) continue;

                    _hubContext.Clients.Group(alert.Slug)
                        .SendAsync("ReceiveAlert", alert.Slug, alert.Milestone, alert.RealTotal, ct);

                    _logger.LogInformation("Alert pushed for slug {Slug}: milestone {Milestone}",
                        alert.Slug, alert.Milestone);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) { _logger.LogError(ex, "Error consuming alert"); }
            }
            consumer.Close();
        }, ct);
    }

    private IConsumer<string, string> BuildConsumer(string groupId)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration.GetConnectionString("Kafka"),
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = true
        };
        return new ConsumerBuilder<string, string>(config).Build();
    }
}