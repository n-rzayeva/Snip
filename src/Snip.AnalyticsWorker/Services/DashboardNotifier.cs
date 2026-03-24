namespace Snip.AnalyticsWorker.Services;

public class DashboardNotifier
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DashboardNotifier> _logger;

    public DashboardNotifier(IConfiguration configuration, ILogger<DashboardNotifier> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(configuration["LinkServiceUrl"]!)
        };
    }

    public async Task NotifyClickAsync(string slug, long totalClicks)
    {
        try
        {
            await _httpClient.PostAsync(
                $"/internal/notify-click?slug={slug}&totalClicks={totalClicks}",
                null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify dashboard for slug {Slug}", ex.Message);
        }
    }
}