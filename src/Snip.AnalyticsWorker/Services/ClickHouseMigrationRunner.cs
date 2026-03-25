using ClickHouse.Client.ADO;
using System.Reflection;

namespace Snip.AnalyticsWorker.Services;

public class ClickHouseMigrationRunner
{
    private readonly string _connectionString;
    private readonly ILogger<ClickHouseMigrationRunner> _logger;

    public ClickHouseMigrationRunner(IConfiguration configuration, ILogger<ClickHouseMigrationRunner> logger)
    {
        _connectionString = configuration.GetConnectionString("ClickHouse")!;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        using var connection = new ClickHouseConnection(_connectionString);
        await connection.OpenAsync();

        await EnsureMigrationsTableAsync(connection);

        var applied = await GetAppliedMigrationsAsync(connection);
        var pending = GetPendingMigrations(applied);

        if (!pending.Any())
        {
            _logger.LogInformation("No pending migrations");
            return;
        }

        foreach (var (name, sql) in pending)
        {
            _logger.LogInformation("Running migration: {Name}", name);

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();

            await RecordMigrationAsync(connection, name);
            _logger.LogInformation("Migration applied: {Name}", name);
        }
    }

    private async Task EnsureMigrationsTableAsync(ClickHouseConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS schema_migrations (
                name String,
                applied_at DateTime DEFAULT now()
            ) ENGINE = MergeTree()
            ORDER BY name";

        await command.ExecuteNonQueryAsync();
    }

    private async Task<HashSet<string>> GetAppliedMigrationsAsync(ClickHouseConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM schema_migrations";

        var applied = new HashSet<string>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            applied.Add(reader.GetString(0));
        }
        return applied;
    }

    private List<(string Name, string Sql)> GetPendingMigrations(HashSet<string> applied)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(r => r.Contains(".Migrations.") && r.EndsWith(".sql"))
            .OrderBy(r => r)
            .ToList();

        var pending = new List<(string, string)>();

        foreach (var resourceName in resourceNames)
        {
            var migrationName = resourceName.Split(".Migrations.").Last();

            if (applied.Contains(migrationName)) continue;

            using var stream = assembly.GetManifestResourceStream(resourceName)!;
            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();

            pending.Add((migrationName, sql));
        }

        return pending;
    }

    private async Task RecordMigrationAsync(ClickHouseConnection connection, string name)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"INSERT INTO schema_migrations (name) VALUES ('{name}')";
        await command.ExecuteNonQueryAsync();
    }
}