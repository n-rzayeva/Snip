using MaxMind.GeoIP2;

namespace Snip.RedirectService.Services;

public class GeoIpService : IDisposable
{
    private readonly DatabaseReader? _reader;
    private readonly ILogger<GeoIpService> _logger;

    public GeoIpService(ILogger<GeoIpService> logger)
    {
        _logger = logger;
        var dbPath = Path.Combine(AppContext.BaseDirectory, "GeoLite2-Country.mmdb");

        if (File.Exists(dbPath))
        {
            _reader = new DatabaseReader(dbPath);
            _logger.LogInformation("GeoIP database loaded successfully");
        }
        else
        {
            _logger.LogWarning("GeoLite2-Country.mmdb not found at {Path}. Country detection disabled.", dbPath);
        }
    }

    public string? GetCountry(string? ipAddress)
    {
        if (_reader is null || string.IsNullOrEmpty(ipAddress)) return null;

        try
        {
            if (_reader.TryCountry(ipAddress, out var response))
            {
                return response?.Country.IsoCode;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("GeoIP lookup failed for {IpAddress}: {Message}", ipAddress, ex.Message);
        }

        return null;
    }

    public void Dispose()
    {
        _reader?.Dispose();
    }
}