using ClickHouse.Client.ADO;
using Microsoft.EntityFrameworkCore;
using Snip.LinkService.Data;
using Snip.LinkService.DTOs;

namespace Snip.LinkService.Services;

public class AnalyticsService
{
    private readonly string _connectionString;
    private readonly SnipDbContext _dbContext;

    public AnalyticsService(IConfiguration configuration, SnipDbContext dbContext)
    {
        _connectionString = configuration.GetConnectionString("ClickHouse")!;
        _dbContext = dbContext;
    }

    public async Task<LinkAnalyticsResponse?> GetLinkAnalyticsAsync(string slug, string userId, int days = 7)
    {
        var linkExists = await _dbContext.Links
            .AnyAsync(l => l.Slug == slug && l.UserId == userId && l.IsActive);

        if (!linkExists) return null;

        using var connection = new ClickHouseConnection(_connectionString);
        await connection.OpenAsync();

        var totalClicks = await GetTotalClicksAsync(connection, slug, days);
        var clicksByHour = await GetClicksByHourAsync(connection, slug, days);
        var clicksByDevice = await GetClicksByDeviceAsync(connection, slug, days);
        var clicksByCountry = await GetClicksByCountryAsync(connection, slug, days);

        return new LinkAnalyticsResponse(slug, totalClicks, clicksByHour, clicksByDevice, clicksByCountry);
    }

    private async Task<long> GetTotalClicksAsync(ClickHouseConnection connection, string slug, int days)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @$"
            SELECT count() 
            FROM click_events 
            WHERE slug = {{slug:String}}
            AND timestamp >= now() - INTERVAL {days} DAY";

        command.Parameters.Add(new ClickHouse.Client.ADO.Parameters.ClickHouseDbParameter
        {
            ParameterName = "slug",
            Value = slug
        });

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    private async Task<IEnumerable<ClicksByHourDto>> GetClicksByHourAsync(ClickHouseConnection connection, string slug, int days)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @$"
            SELECT 
                toStartOfHour(timestamp) as hour,
                count() as clicks
            FROM click_events
            WHERE slug = {{slug:String}}
            AND timestamp >= now() - INTERVAL {days} DAY
            GROUP BY hour
            ORDER BY hour ASC";

        command.Parameters.Add(new ClickHouse.Client.ADO.Parameters.ClickHouseDbParameter
        {
            ParameterName = "slug",
            Value = slug
        });

        var results = new List<ClicksByHourDto>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new ClicksByHourDto(
                reader.GetDateTime(0),
                Convert.ToInt64(reader.GetValue(1))
            ));
        }
        return results;
    }

    private async Task<IEnumerable<ClicksByDeviceDto>> GetClicksByDeviceAsync(ClickHouseConnection connection, string slug, int days)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @$"
            SELECT
                multiIf(
                    user_agent LIKE '%Mobile%', 'Mobile',
                    user_agent LIKE '%Tablet%', 'Tablet',
                    'Desktop'
                ) as device,
                count() as clicks
            FROM click_events
            WHERE slug = {{slug:String}}
            AND timestamp >= now() - INTERVAL {days} DAY
            GROUP BY device
            ORDER BY clicks DESC";

        command.Parameters.Add(new ClickHouse.Client.ADO.Parameters.ClickHouseDbParameter
        {
            ParameterName = "slug",
            Value = slug
        });
        
        var results = new List<ClicksByDeviceDto>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new ClicksByDeviceDto(
                reader.GetString(0),
                Convert.ToInt64(reader.GetValue(1))
            ));
        }
        return results;
    }

    private async Task<IEnumerable<ClicksByCountryDto>> GetClicksByCountryAsync(
    ClickHouseConnection connection, string slug, int days)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @$"
            SELECT
                coalesce(country, 'Unknown') as country,
                count() as clicks
            FROM click_events
            WHERE slug = {{slug:String}}
            AND timestamp >= now() - INTERVAL {days} DAY
            GROUP BY country
            ORDER BY clicks DESC
            LIMIT 10";

        command.Parameters.Add(new ClickHouse.Client.ADO.Parameters.ClickHouseDbParameter
        {
            ParameterName = "slug",
            Value = slug
        });

        var results = new List<ClicksByCountryDto>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new ClicksByCountryDto(
                reader.GetString(0),
                Convert.ToInt64(reader.GetValue(1))
            ));
        }
        return results;
    }
}