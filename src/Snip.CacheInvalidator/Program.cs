using Snip.CacheInvalidator;
using Snip.CacheInvalidator.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<CacheInvalidatorService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
