using ClickHouse.Client.ADO;
using ClickHouse.Client.Copy;
using Snip.Shared.Events;

namespace Snip.AnalyticsWorker.Services;

public class ClickHouseWriterService
{
    private readonly string _connectionString;

    public ClickHouseWriterService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ClickHouse")!;
    }

    public async Task WriteClickEventAsync(ClickEvent clickEvent)
    {
        using var connection = new ClickHouseConnection(_connectionString);
        await connection.OpenAsync();

        using var copy = new ClickHouseBulkCopy(connection)
        {
            DestinationTableName = "click_events",
            BatchSize = 1
        };

        await copy.InitAsync();

        var rows = new List<object?[]>
        {
            new object?[]
            {
                clickEvent.Slug,
                clickEvent.DestinationUrl,
                clickEvent.Timestamp,
                clickEvent.IpAddress,
                clickEvent.UserAgent,
                clickEvent.Referer
            }
        };

        await copy.WriteToServerAsync(rows);
    }

    public async Task<long> GetTotalClicksAsync(string slug)
    {
        using var connection = new ClickHouseConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT count() FROM click_events WHERE slug = '{slug}'";

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }
}