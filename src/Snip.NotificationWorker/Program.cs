using Snip.NotificationWorker;
using Snip.NotificationWorker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<NotificationService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
