using Snip.AnalyticsWorker;
using Snip.AnalyticsWorker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<KafkaConsumerService>();
builder.Services.AddSingleton<ClickHouseWriterService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
