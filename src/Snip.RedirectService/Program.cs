using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Snip.RedirectService.Data;
using Snip.RedirectService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<RedirectDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));

builder.Services.AddSingleton<CacheService>();

var app = builder.Build();

app.MapGet("/r/{slug}", async (string slug, RedirectDbContext db, CacheService cache) =>
{
    // Step 1: check Redis
    var cachedUrl = await cache.GetAsync(slug);
    if (cachedUrl is not null)
    {
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

    return Results.Redirect(link.DestinationUrl, permanent: false);
});

app.Run();