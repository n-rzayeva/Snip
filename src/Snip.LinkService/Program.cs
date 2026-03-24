using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Snip.LinkService.Data;
using Snip.LinkService.DTOs;
using Snip.Shared.Models;
using Snip.LinkService.Services;
using Snip.Shared;
using Snip.Shared.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SnipDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddSingleton<SlugService>();
builder.Services.AddSingleton<KafkaProducerService>();
builder.Services.AddSingleton<AnalyticsService>();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI();

app.MapHub<Snip.LinkService.Hubs.ClickHub>("/hubs/clicks");

// POST /api/links
app.MapPost("/api/links", async (CreateLinkRequest request, SnipDbContext db, SlugService slugService) =>
{
    var link = new Link
    {
        Id = Guid.NewGuid(),
        Slug = slugService.Generate(),
        DestinationUrl = request.DestinationUrl,
        CreatedAt = DateTime.UtcNow,
        IsActive = true
    };

    db.Links.Add(link);
    await db.SaveChangesAsync();

    var response = new LinkResponse(link.Id, link.Slug, link.DestinationUrl, link.CreatedAt, link.IsActive);
    return Results.Created($"/api/links/{link.Id}", response);
});

// GET /api/links/{id}
app.MapGet("/api/links/{id:guid}", async (Guid id, SnipDbContext db) =>
{
    var link = await db.Links.FindAsync(id);
    if (link is null) return Results.NotFound();

    return Results.Ok(new LinkResponse(link.Id, link.Slug, link.DestinationUrl, link.CreatedAt, link.IsActive));
});

// PUT /api/links/{id}
app.MapPut("/api/links/{id:guid}", async (Guid id, UpdateLinkRequest request, SnipDbContext db, KafkaProducerService kafka) =>
{
    var link = await db.Links.FindAsync(id);
    if (link is null) return Results.NotFound();

    link.DestinationUrl = request.DestinationUrl;
    link.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();

    await kafka.PublishAsync(Topics.LinkUpdated, link.Slug, new LinkUpdatedEvent
    {
        Slug = link.Slug,
        NewDestinationUrl = link.DestinationUrl,
        Timestamp = DateTime.UtcNow
    });

    return Results.Ok(new LinkResponse(link.Id, link.Slug, link.DestinationUrl, link.CreatedAt, link.IsActive));
});

// DELETE /api/links/{id}
app.MapDelete("/api/links/{id:guid}", async (Guid id, SnipDbContext db, KafkaProducerService kafka) =>
{
    var link = await db.Links.FindAsync(id);
    if (link is null) return Results.NotFound();

    link.IsActive = false;
    link.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();

    await kafka.PublishAsync(Topics.LinkDeleted, link.Slug, new LinkDeletedEvent
    {
        Slug = link.Slug,
        Timestamp = DateTime.UtcNow
    });

    return Results.NoContent();
});

// GET /api/links/{slug}/analytics
app.MapGet("/api/links/{slug}/analytics", async (string slug, AnalyticsService analytics) =>
{
    var result = await analytics.GetLinkAnalyticsAsync(slug);
    return Results.Ok(result);
});

app.MapPost("/internal/notify-click", async (
    string slug,
    long totalClicks,
    IHubContext<Snip.LinkService.Hubs.ClickHub> hubContext) =>
{
    await hubContext.Clients.Group(slug).SendAsync("ReceiveClickUpdate", slug, totalClicks);
    return Results.Ok();
});

app.Run();