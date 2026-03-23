using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Confluent.Kafka;
using Snip.RedirectService.Data;
using Snip.RedirectService.Services;
using Snip.Shared;
using Snip.Shared.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<RedirectDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));

builder.Services.AddSingleton<CacheService>();
builder.Services.AddSingleton<KafkaProducerService>();

var app = builder.Build();

app.MapGet("/r/{slug}", async (
    string slug, 
    HttpContext http,
    RedirectDbContext db, 
    CacheService cache,
    KafkaProducerService kafka) =>
{
    // Step 1: check Redis
    var cachedUrl = await cache.GetAsync(slug);
    if (cachedUrl is not null)
    {
        await PublishClickEvent(kafka, slug, cachedUrl, http);
        return Results.Redirect(cachedUrl, permanent: false);
    }

    // Step 2: check PostgreSQL
    var link = await db.Links.FirstOrDefaultAsync(l => l.Slug == slug && l.IsActive);
    if (link is null)
    {
        return Results.NotFound();
    }

    // Step 3: warm the cache
    await cache.SetAsync(slug, link.DestinationUrl);

    await PublishClickEvent(kafka, slug, link.DestinationUrl, http);
    return Results.Redirect(link.DestinationUrl, permanent: false);
});

app.Run();

static async Task PublishClickEvent(KafkaProducerService kafka, string slug, string destinationUrl, HttpContext http)
{
    var clickEvent = new ClickEvent
    {
        Slug = slug,
        DestinationUrl = destinationUrl,
        Timestamp = DateTime.UtcNow,
        IpAddress = http.Connection.RemoteIpAddress?.ToString(),
        UserAgent = http.Request.Headers.UserAgent.ToString(),
        Referer = http.Request.Headers.Referer.ToString()
    };

    await kafka.PublishAsync(Topics.ClickEvents, slug, clickEvent);
}