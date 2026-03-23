using Microsoft.EntityFrameworkCore;
using Snip.LinkService.Data;
using Snip.LinkService.DTOs;
using Snip.Shared.Models;
using Snip.LinkService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SnipDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddSingleton<SlugService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

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
app.MapPut("/api/links/{id:guid}", async (Guid id, UpdateLinkRequest request, SnipDbContext db) =>
{
    var link = await db.Links.FindAsync(id);
    if (link is null) return Results.NotFound();

    link.DestinationUrl = request.DestinationUrl;
    link.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();

    return Results.Ok(new LinkResponse(link.Id, link.Slug, link.DestinationUrl, link.CreatedAt, link.IsActive));
});

// DELETE /api/links/{id}
app.MapDelete("/api/links/{id:guid}", async (Guid id, SnipDbContext db) =>
{
    var link = await db.Links.FindAsync(id);
    if (link is null) return Results.NotFound();

    link.IsActive = false;
    link.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.Run();