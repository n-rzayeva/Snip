using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Snip.LinkService.Data;
using Snip.LinkService.DTOs;
using Snip.Shared.Models;
using Snip.LinkService.Services;
using Snip.Shared;
using Snip.Shared.Auth;
using Snip.Shared.Events;
using Serilog;
using Snip.Shared.Telemetry;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddDbContext<SnipDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddSingleton<SlugService>();
builder.Services.AddSingleton<KafkaProducerService>();
builder.Services.AddSingleton<AnalyticsService>();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var jwtSettings = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(jwtSettings["Secret"]!))
    };
});
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<SnipDbContext>("postgres");

builder.Services.AddSnipTracing("Snip.LinkService");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SnipDbContext>();
    db.Database.Migrate();
}

app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapHub<Snip.LinkService.Hubs.ClickHub>("/hubs/clicks");

// POST /api/links
app.MapPost("/api/links", async (CreateLinkRequest request, HttpContext http, SnipDbContext db, SlugService slugService) =>
{
    var userId = http.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var link = new Link
    {
        Id = Guid.NewGuid(),
        Slug = slugService.Generate(),
        DestinationUrl = request.DestinationUrl,
        UserId = userId,
        CreatedAt = DateTime.UtcNow,
        IsActive = true
    };

    db.Links.Add(link);
    await db.SaveChangesAsync();

    var response = new LinkResponse(link.Id, link.Slug, link.DestinationUrl, link.CreatedAt, link.IsActive);
    return Results.Created($"/api/links/{link.Id}", response);
});

// GET /api/links/{id}
app.MapGet("/api/links/{id:guid}", async (Guid id, HttpContext http, SnipDbContext db) =>
{
    var userId = http.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var link = await db.Links.FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);
    if (link is null) return Results.NotFound();

    return Results.Ok(new LinkResponse(link.Id, link.Slug, link.DestinationUrl, link.CreatedAt, link.IsActive));
});

// PUT /api/links/{id}
app.MapPut("/api/links/{id:guid}", async (Guid id, UpdateLinkRequest request, HttpContext http, SnipDbContext db, KafkaProducerService kafka) =>
{
    var userId = http.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var link = await db.Links.FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);
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
app.MapDelete("/api/links/{id:guid}", async (Guid id, HttpContext http, SnipDbContext db, KafkaProducerService kafka) =>
{
    var userId = http.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var link = await db.Links.FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);
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

// GET /api/links
app.MapGet("/api/links", async (HttpContext http, SnipDbContext db) =>
{
    var userId = http.GetUserId();
    if (userId is null) return Results.Unauthorized();

    var links = await db.Links
        .Where(l => l.UserId == userId && l.IsActive)
        .OrderByDescending(l => l.CreatedAt)
        .Select(l => new LinkResponse(l.Id, l.Slug, l.DestinationUrl, l.CreatedAt, l.IsActive))
        .ToListAsync();

    return Results.Ok(links);
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

app.UseSerilogRequestLogging();
app.Run();