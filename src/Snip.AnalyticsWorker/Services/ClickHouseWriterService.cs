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

    public async Task EnsureTableExistsAsync()
    {
        using var connection = new ClickHouseConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS click_events (
                slug String,
                destination_url String,
                timestamp DateTime,
                ip_address Nullable(String),
                user_agent Nullable(String),
                referer Nullable(String)
            ) ENGINE = MergeTree()
            ORDER BY (slug, timestamp)";

        await command.ExecuteNonQueryAsync();
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
}