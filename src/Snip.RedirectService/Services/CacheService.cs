using StackExchange.Redis;

namespace Snip.RedirectService.Services;

public class CacheService
{
    private readonly IDatabase _redis;
    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(30);

    public CacheService(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    public async Task<string?> GetAsync(string slug)
    {
        var value = await _redis.StringGetAsync(slug);
        return value.HasValue ? value.ToString() : null;
    }

    public async Task SetAsync(string slug, string destinationUrl)
    {
        await _redis.StringSetAsync(slug, destinationUrl, _ttl);
    }

    public async Task DeleteAsync(string slug)
    {
        await _redis.KeyDeleteAsync(slug);
    }
}