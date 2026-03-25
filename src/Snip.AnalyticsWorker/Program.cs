using Snip.AnalyticsWorker;
using Snip.AnalyticsWorker.Services;
using Serilog;
using Snip.Shared.Telemetry;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog();
builder.Services.AddSingleton<KafkaConsumerService>();
builder.Services.AddSingleton<ClickHouseMigrationRunner>();
builder.Services.AddSingleton<ClickHouseWriterService>();
builder.Services.AddSingleton<DashboardNotifier>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddSnipTracing("Snip.AnalyticsWorker", isWebService: false);

var host = builder.Build();
host.Run();
